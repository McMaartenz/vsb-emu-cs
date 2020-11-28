using System;
using System.Collections.Generic;

public class EngineStack
{
	private Stack<string> x;
	public EngineStack()
	{
		x = new Stack<string>();
	}

	public void Push(string input)
	{
		x.Push(input);
	}

	public void Pop(out string output)
	{
		output = x.Pop();
	}

	public bool HasNext()
	{
		return x.Count != 0;
	}
}
