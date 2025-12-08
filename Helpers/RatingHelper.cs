using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MasterServicePlatform.Web.Helpers
{
    public static class RatingHelper
    {
        public static IHtmlContent RenderStars(this IHtmlHelper html, double rating)
        {
            int fullStars = (int)Math.Floor(rating);
            bool hasHalf = rating - fullStars >= 0.5;
            int emptyStars = 5 - fullStars - (hasHalf ? 1 : 0);

            string starsHtml = string.Concat(
                new string('★', fullStars),
                hasHalf ? "⯨" : "",
                new string('☆', emptyStars)
            );

            return new HtmlString($"<span style='color:#FFD700;font-size:1.1em'>{starsHtml}</span>");
        }
    }
}
