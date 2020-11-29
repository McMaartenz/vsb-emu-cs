using System.Collections.Generic;

public class EngineMemory
{
	private List<string> x;

	/* EngineMemory(): Class constructor, creates a memory space */
	public EngineMemory()
	{
		x = new List<string>();
	}

	/* Add(): Adds a value to the memory, and returns the memory address it's at */
	public int Add(string value)
	{
		x.Add(value);
		return x.Count;
	}

	/* Remove(): Removes a given amount of the memory space */
	public void Remove(int amount)
	{ //FIXME This doesn\t work
		x.RemoveRange(x.Count - amount, amount);
	}

	/* Set(): Sets the space at a given memory address to the given value */
	public void Set(int index, string value)
	{
		x[index] = value;
	}

	/* Exists(): Returns whether or not the index is in bounds */
	public bool Exists(int index)
	{
		return x.Count > index && index >= 0;
	}
}
