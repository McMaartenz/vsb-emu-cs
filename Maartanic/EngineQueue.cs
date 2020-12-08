using System.Collections.Generic;

internal class EngineQueue
{
	private readonly Queue<string> x;

	// EngineQueue(): Class constructor, creates a queue
	internal EngineQueue()
	{
		x = new Queue<string>();
	}

	// Enqueue(): Enqueue to queue
	internal void Enqueue(string input)
	{
		x.Enqueue(input);
	}

	// Dequeue(): Dequeue from queue
	internal void Dequeue(out string output)
	{
		output = x.Dequeue();
	}

	// HasNext(): Returns whether or not the queue may be dequeued from
	internal bool HasNext()
	{
		return x.Count != 0;
	}
}
