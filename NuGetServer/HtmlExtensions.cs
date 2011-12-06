using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;

namespace NuGetServer {
    public static class HtmlExtensions {
        public static string GetFieldName<TModel, TResult>(this HtmlHelper<TModel> html, Expression<Func<TModel, TResult>> expression) {
            return html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(ExpressionHelper.GetExpressionText(expression));
        }

        public static string GetFieldId<TModel, TResult>(this HtmlHelper<TModel> html, Expression<Func<TModel, TResult>> expression) {
            return html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldId(ExpressionHelper.GetExpressionText(expression));
        }
    }
}