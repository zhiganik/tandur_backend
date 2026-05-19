namespace Core.DTOs.Categories;

public class CreateCategoryRequest
{
    public string Name { get; init; } = string.Empty;
    public bool IsVisible { get; init; } = true;
}

public class UpdateCategoryRequest
{
    public string Name { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsVisible { get; init; }
}

public class PatchCategoryRequest
{
    public bool? IsVisible { get; init; }
    public int? SortOrder { get; init; }
}
