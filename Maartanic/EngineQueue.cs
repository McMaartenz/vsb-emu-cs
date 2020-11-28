using System.Collections.Generic;

public class EngineQueue
{
	private Queue<string> x;

	public EngineQueue()
	{
		x = new Queue<string>();
	}

	public void Enqueue(string input)
	{
		x.Enqueue(input);
	}

	public void Dequeue(out string output)
	{
		output = x.Dequeue();
	}

	public bool HasNext()
	{
		return x.Count != 0;
	}
}
