using System.Diagnostics;
using Newtonsoft.Json.Linq;

public static partial class Program
{
	static void Main(string[] args)
	{
		Stopwatch sw = Stopwatch.StartNew();

		// Carregar o arquivo do desafio da Rinha
		var jsonFile = File.ReadAllText("files/" + args[0]);
		
		dynamic jsonObject = JObject.Parse(jsonFile);
		Execute(jsonObject.expression, new Dictionary<string, object>());
		sw.Stop();
		Console.WriteLine($"Tempo de Execução: {sw.Elapsed}");
	}

	public static dynamic Execute(dynamic term, Dictionary<string, object> memory)
	{
		switch (term.kind.Value)
		{
			case "Binary":
				var lhs = Execute(term.lhs, memory);
				var rhs = Execute(term.rhs, memory);

				switch (term.op.Value)
				{
					case "Add": return lhs + rhs;
					case "Sub": return lhs - rhs;
					case "Mul": return lhs * rhs;
					case "Div": return lhs / rhs;
					case "Rem": return lhs % rhs;

					case "Eq": return ((object)lhs).ToString().Equals(((object)rhs).ToString());
					case "Neq": return lhs != rhs;
					
					case "Lt": return lhs < rhs;
					case "Gt": return lhs > rhs;
					case "Lte": return lhs <= rhs;
					case "Gte": return lhs >= rhs;

					case "And": return lhs && rhs;
					case "Or": return lhs || rhs;
				}
				throw new ApplicationException("invalid binary operator!");

			case "Call":
				var callee = Execute(term.callee, memory);

				var functionScope = CreateFunctionScope(memory);
				for (int i = 0; i < callee.parameters.Count; i++)
				{
					var paramName = callee.parameters[i].text.Value;
					var paramValue = Execute(term.arguments[i], memory);
					
					SetVariableValue(functionScope, paramName, paramValue);
				}
				
				return Execute(callee.value, functionScope);

			case "Int":
			case "Str": return term.value.Value;

			case "Function":
				return term; // declaration only

			case "If":
				var condition = Execute(term.condition, memory);
				if (condition)
					return Execute(term.then, memory);
				else
					return Execute(term.otherwise, memory);

			case "Let":
				memory[term.name.text.Value] = Execute(term.value, memory);
				return Execute(term.next, memory);

			case "Print":
				var printTerm = Execute(term.value, memory);
				Console.WriteLine(printTerm.ToString());
				return printTerm; // allows for print(print("some string"))

			case "Var": 
				return memory[term.text.Value];

		}

		return term;
	}

	static Dictionary<string, object> CreateFunctionScope(Dictionary<string, object> currentMemory)
	{
		var newScope = new Dictionary<string, object>();

		foreach (var key in currentMemory.Keys)
		{
			newScope.Add(key, currentMemory[key]);
		}

		return newScope;
	}

	static void SetVariableValue(Dictionary<string, object> memory, string paramName, object paramValue)
	{
		if (!memory.ContainsKey(paramName))
			memory.Add(paramName, paramValue);
		else
			memory[paramName] = paramValue;
	}

}
