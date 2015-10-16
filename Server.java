package network;

import java.io.IOException;
import java.net.ServerSocket;
import java.net.Socket;
import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.BlockingQueue;
import java.util.concurrent.LinkedBlockingQueue;

public class Server implements Runnable
{
	private ServerSocket serverSocket;
	private int clientIndexer;

	private HashMap<Integer, ServerThread> serverThreads;
	private BlockingQueue<NetworkObject> queue;

	public Server()
	{
		serverThreads = new HashMap<>();
		queue = new LinkedBlockingQueue<>();
	}

	public void run()
	{
		try
		{
			clientIndexer = 0;
			serverSocket = new ServerSocket(1337);
			System.out.println("Server: Standing by...");

			while (true)
			{
				Socket socket = serverSocket.accept();
				ServerThread serverThread = new ServerThread(this, socket, clientIndexer);
				putServerThread(clientIndexer, serverThread);
				new Thread(serverThread).start();
				clientIndexer++;
			}
		}
		catch (Exception e)
		{
			e.printStackTrace();
		}
		finally
		{
			try
			{
				serverSocket.close();
			}
			catch (IOException e)
			{
				e.printStackTrace();
			}
		}
	}

	public void putServerThread(int i, ServerThread serverThread)
	{
		synchronized (serverThreads)
		{
			serverThreads.put(i, serverThread);
		}
	}

	public void removeServerThread(int i)
	{
		synchronized (serverThreads)
		{
			serverThreads.remove(i);
		}
	}

	public void sendNetworkObject(NetworkObject no)
	{
		if (no.getClientID() >= 0)
		{
			if (serverThreads.containsKey(no.getClientID()))
				serverThreads.get(no.getClientID()).sendObject(no.getObject());
			else
				System.out.println("Server: Error - Client " + no.getClientID() + " does not exist.");
		}
		else
		{
			synchronized (serverThreads)
			{
				for (Map.Entry<Integer, ServerThread> entry : serverThreads.entrySet())
				{
					entry.getValue().sendObject(no.getObject());
				}
			}
		}
	}

	public void produceNetworkObject(int clientID, Object o) throws InterruptedException
	{
		queue.put(new NetworkObject(clientID, o));
	}

	public NetworkObject consumeNetworkObject() throws InterruptedException
	{
		return queue.take();
	}
}
