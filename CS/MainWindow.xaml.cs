using DevExpress.Data.Filtering;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Data;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Windows;

namespace InfiniteAsyncSourceODataSample {
    public partial class MainWindow : ThemedWindow {
        static string[] Properties = new string[] { "Subject", "TechnologyName", "ProductName" };
        static char[] Separators = new char[] { ' ', ',', ';' };

        static FetchRowsResult FetchRows(FetchRowsAsyncEventArgs e) {
            if (ReferenceEquals(e.Filter, null))
                return new FetchRowsResult(null, false);
            var queryable = GetIssueDataQueryable().Where(MakeFilterExpression(e.Filter));
            return queryable
                .Skip(e.Skip)
                .Take(42)
                .ToArray();
        }
        static Expression<Func<SCIssuesDemo, bool>> MakeFilterExpression(CriteriaOperator filter) {
            var converter = new GridFilterCriteriaToExpressionConverter<SCIssuesDemo>();
            return converter.Convert(filter);
        }
        static IQueryable<SCIssuesDemo> GetIssueDataQueryable() {
            var context = new SCEntities(new Uri("https://demos.devexpress.com/Services/WcfLinqSC/WcfSCService.svc/"));
            return context.SCIssuesDemo;
        }
        static CriteriaOperator GetFilter(string text) {
            if (string.IsNullOrEmpty(text))
                return null;
            var values = text.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length == 0)
                return null;
            return new GroupOperator(GroupOperatorType.Or, Properties.SelectMany(prov => values.Select(val => new FunctionOperator(FunctionOperatorType.Contains, new OperandProperty(prov), new OperandValue(val)))));
        }
        static void AssignFilter(AutoSuggestEdit editor, string searchString) {
            if (editor.PopupElement is GridControl grid)
                grid.FixedFilter = GetFilter(searchString);
        }

        public MainWindow() {
            InitializeComponent();
            var source = new InfiniteAsyncSource() { ElementType = typeof(SCIssuesDemo) };
            source.FetchRows += (o, e) => e.Result = Task.Run(() => FetchRows(e));
            autoSuggestEdit.ItemsSource = source;
            Closing += (o, e) => source.Dispose();
        }

        void QuerySubmitted(object sender, AutoSuggestEditQuerySubmittedEventArgs e) {
            AssignFilter(autoSuggestEdit, e.Text);
        }
        void PopupOpened(object sender, RoutedEventArgs e) {
            AssignFilter(autoSuggestEdit, autoSuggestEdit.EditText);
        }
    }
}