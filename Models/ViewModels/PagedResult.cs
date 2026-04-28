namespace Bevera.Models.ViewModels
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 15;

        public int TotalItems { get; set; }

        public int TotalPages =>
            PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalItems / (double)PageSize);

        public bool HasPrevious => Page > 1;

        public bool HasNext => Page < TotalPages;
    }
}