using System.ComponentModel.DataAnnotations;

namespace Model.Math
{
	public enum MathOperation
	{
		[Display(Name = "+")]
		ADD,
		[Display(Name = "-")]
		SUBTRACT,
		[Display(Name = "x")]
		MULTIPLY,
		[Display(Name = "/")]
		DIVIDE,
	};
}
