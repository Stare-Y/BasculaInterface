namespace Core.Application.DTOs
{
    public class BasculaLectureDto
    {
        public double Weight { get; set; }
        public string UsingWeight { get; set; } = string.Empty;
        public override string ToString()
        {
            return $"{Weight} kg - Usando: {UsingWeight}";
        }
    }
}
