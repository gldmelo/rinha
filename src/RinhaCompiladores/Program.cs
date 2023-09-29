using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static partial class Program
{
	public static Dictionary<string, object> FunctionCache = new Dictionary<string, object>();

	static void Main(string[] args)
	{
		Stopwatch sw = Stopwatch.StartNew();

		// Carregar o arquivo do desafio da Rinha
		var programFilename = args.Length > 0 ? args[0] : "/var/rinha/source.rinha.json";
		var jsonFile = File.ReadAllText(programFilename);
		
		var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, MaxDepth = 8096 };

		dynamic jsonObject = JsonConvert.DeserializeObject<JObject>(jsonFile, settings); //JObject.Parse(jsonFile);
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

					case "Eq": return lhs == rhs;
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
				Tuple<dynamic, Dictionary<string, object>> callee = Execute(term.callee, memory);

				var cachedFuncionParameter = new List<object>();
				var functionScope = CreateFunctionScope(callee.Item2);
				for (int i = 0; i < callee.Item1.parameters.Count; i++)
				{
					var paramName = callee.Item1.parameters[i].text.Value;
					var paramValue = Execute(term.arguments[i], memory);
					cachedFuncionParameter.Add(paramValue);

					SetVariableValue(functionScope, paramName, paramValue);
				}

				#region Cache do resultado da função usando o conceito de Dynamic Programming (Programação Dinâmica)
				// Cache é desativado para funções anônimas
				if (term.callee.text != null)
				{
					var cacheKey = $"{term.callee.text.Value}|{string.Join(",", cachedFuncionParameter.ToArray())}";
					var cachedFuncion = FunctionCache.ContainsKey(cacheKey) ? FunctionCache[cacheKey] : null;
					if (cachedFuncion != null)
						return cachedFuncion;

					var functionResult = Execute(callee.Item1.value, functionScope);
					FunctionCache[cacheKey] = functionResult;
					return functionResult;
				}
				else
					return Execute(callee.Item1.value, functionScope);
				#endregion

			case "Bool":
			case "Int":
			case "Str": return term.value.Value;

			case "File":
				return Execute(term.expression, memory);

			case "First":
				var tupleFirst = Execute(term.value, memory);
				return tupleFirst.Item1;

			case "Function":
				return new Tuple<dynamic, Dictionary<string, object>>(term, memory); // function + scope

			case "If":
				var condition = Execute(term.condition, memory);
				if (condition)
					return Execute(term.then, memory);
				else
					return Execute(term.otherwise, memory);

			case "Let":
				var letReturn = Execute(term.value, memory);
				memory[term.name.text.Value] = letReturn;
				return Execute(term.next, memory);

			case "Print":
				var printTerm = Execute(term.value, memory);
				
				if (printTerm is Tuple<object, object>)
				{
					var tupleTerm = (Tuple<object, object>)printTerm;
					Console.WriteLine($"({tupleTerm.Item1}, {tupleTerm.Item2})");
				}
				else
				{
					if (printTerm is JToken)
					{
						if (printTerm.kind.Value == "Function")
							Console.WriteLine("<#closure>");
					}
					else
						Console.WriteLine(printTerm.ToString());
				}
				
				return printTerm; // allows for print(print("some string"))

			case "Second":
				var tupleSecond = Execute(term.value, memory);
				return tupleSecond.Item2;

			case "Tuple":
				var firstTerm = Execute(term.first, memory);
				var secondTerm = Execute(term.second, memory);
				return new Tuple<object, object>(firstTerm, secondTerm);

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
