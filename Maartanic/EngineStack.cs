using System.Collections.Generic;

public class EngineStack
{
	private Stack<string> x;
	
	// EngineStack(): Class constructor, creates a stack
	public EngineStack()
	{
		x = new Stack<string>();
	}

	// Push(): Push a value onto stack
	public void Push(string input)
	{
		x.Push(input);
	}

	// Pop(): Pop a value from stack
	public void Pop(out string output)
	{
		output = x.Pop();
	}

	// HasNext(): Returns whether or not the stack may be popped from
	public bool HasNext()
	{
		return x.Count != 0;
	}
}
