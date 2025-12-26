namespace Core.Application.DTOs
{
    public class GenericResponse<T> 
    {
        public T? Data { get; set; }
        public string? Message { get; set; }
    }
}
