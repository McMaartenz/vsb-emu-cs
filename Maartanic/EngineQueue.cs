using System.Collections.Generic;

public class EngineQueue
{
	private Queue<string> x;

	// EngineQueue(): Class constructor, creates a queue
	public EngineQueue()
	{
		x = new Queue<string>();
	}

	// Enqueue(): Enqueue to queue
	public void Enqueue(string input)
	{
		x.Enqueue(input);
	}

	// Dequeue(): Dequeue from queue
	public void Dequeue(out string output)
	{
		output = x.Dequeue();
	}

	// HasNext(): Returns whether or not the queue may be dequeued from
	public bool HasNext()
	{
		return x.Count != 0;
	}
}
