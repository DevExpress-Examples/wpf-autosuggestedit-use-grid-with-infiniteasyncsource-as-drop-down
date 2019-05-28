Imports DevExpress.Data.Filtering
Imports DevExpress.Xpf.Core
Imports DevExpress.Xpf.Data
Imports DevExpress.Xpf.Editors
Imports DevExpress.Xpf.Grid
Imports System
Imports System.Linq
Imports System.Linq.Expressions
Imports System.Threading.Tasks
Imports System.Windows

Namespace InfiniteAsyncSourceODataSample
    Partial Public Class MainWindow
        Inherits ThemedWindow

        Private Shared Properties() As String = { "Subject", "TechnologyName", "ProductName" }
        Private Shared Separators() As Char = { " "c, ","c, ";"c }

        Private Shared Function FetchRows(ByVal e As FetchRowsAsyncEventArgs) As FetchRowsResult
            If ReferenceEquals(e.Filter, Nothing) Then
                Return New FetchRowsResult(Nothing, False)
            End If
            Dim queryable = GetIssueDataQueryable().Where(MakeFilterExpression(e.Filter))
            Return queryable.Skip(e.Skip).Take(42).ToArray()
        End Function
        Private Shared Function MakeFilterExpression(ByVal filter As CriteriaOperator) As Expression(Of Func(Of SCIssuesDemo, Boolean))
            Dim converter = New GridFilterCriteriaToExpressionConverter(Of SCIssuesDemo)()
            Return converter.Convert(filter)
        End Function
        Private Shared Function GetIssueDataQueryable() As IQueryable(Of SCIssuesDemo)
            Dim context = New SCEntities(New Uri("https://demos.devexpress.com/Services/WcfLinqSC/WcfSCService.svc/"))
            Return context.SCIssuesDemo
        End Function
        Private Shared Function GetFilter(ByVal text As String) As CriteriaOperator
            If String.IsNullOrEmpty(text) Then
                Return Nothing
            End If
            Dim values = text.Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            If values.Length = 0 Then
                Return Nothing
            End If
            Return New GroupOperator(GroupOperatorType.Or, Properties.SelectMany(Function(prov) values.Select(Function(val) New FunctionOperator(FunctionOperatorType.Contains, New OperandProperty(prov), New OperandValue(val)))))
        End Function
        Private Shared Sub AssignFilter(ByVal editor As AutoSuggestEdit, ByVal searchString As String)
            If TypeOf editor.PopupElement Is GridControl Then
                Dim grid = DirectCast(editor.PopupElement, GridControl)
                grid.FixedFilter = GetFilter(searchString)
            End If
        End Sub

        Public Sub New()
            InitializeComponent()
            Dim source = New InfiniteAsyncSource() With {.ElementType = GetType(SCIssuesDemo)}
            AddHandler source.FetchRows, Sub(o, e) e.Result = Task.Run(Function() FetchRows(e))
            autoSuggestEdit.ItemsSource = source
            AddHandler Closing, Sub(o, e) source.Dispose()
        End Sub

        Private Sub QuerySubmitted(ByVal sender As Object, ByVal e As AutoSuggestEditQuerySubmittedEventArgs)
            AssignFilter(autoSuggestEdit, e.Text)
        End Sub
        Private Sub PopupOpened(ByVal sender As Object, ByVal e As RoutedEventArgs)
            AssignFilter(autoSuggestEdit, autoSuggestEdit.EditText)
        End Sub
    End Class
End Namespace