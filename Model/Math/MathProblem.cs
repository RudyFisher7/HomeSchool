using System.ComponentModel.DataAnnotations;

namespace Model.Math
{
    public class MathProblem
    {
        public int Id { get; set; }
        [Display(Name = "Answere Boyyyyyy")]
        public double Answer { get; set; }

        //[Required]//value types are inherently required
        [Range(-100.0, 100.0)]
        public double Factor1 { get; set; }
        public double Factor2 { get; set; }
        public MathOperation Operation { get; set; }

        [StringLength(100, MinimumLength = 0)]
        public string Description { get; set; } = string.Empty;
    }
}
