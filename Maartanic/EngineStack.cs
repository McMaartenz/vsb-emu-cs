using System.Collections.Generic;

internal class EngineStack
{
	private Stack<string> x;
	
	// EngineStack(): Class constructor, creates a stack
	internal EngineStack()
	{
		x = new Stack<string>();
	}

	// Push(): Push a value onto stack
	internal void Push(string input)
	{
		x.Push(input);
	}

	// Pop(): Pop a value from stack
	internal void Pop(out string output)
	{
		output = x.Pop();
	}

	// HasNext(): Returns whether or not the stack may be popped from
	internal bool HasNext()
	{
		return x.Count != 0;
	}
}
