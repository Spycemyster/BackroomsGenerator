using Godot;
using System;
using System.Collections.Generic;

public class Level1 : Spatial
{
	public class LevelNode
	{
		public enum Direction
		{
			NORTH,
			EAST,
			SOUTH,
			WEST,
			MAX,
		}
		public LevelNode Parent
		{
			get {return mParent;}
		}
		public (int x, int z) Position
		{
			get { return mPosition; }
		}
		public bool IsExplored
		{
			get {return mIsExplored;}
			set {mIsExplored = value;}
		}
		private List<Direction> mConnections;
		private (int x, int z) mPosition;
		private bool mIsExplored;
		private LevelNode mParent;

		public LevelNode((int x, int z) position, LevelNode parent = null)
		{
			this.mPosition = position;
			mConnections = new List<Direction>();
			mIsExplored = false;
			mParent = parent;
		}

		public bool AddDirection(Direction direction)
		{
			if (mConnections.Contains(direction))
				return false;

			mConnections.Add(direction);
			return true;
		}

		public Direction GetDirectionTo(LevelNode node)
		{
			(int x, int z) to = node.Position;

			int dx = to.x - mPosition.x;
			int dz = to.z - mPosition.z;

			if (dx > 0)
				return Direction.EAST;
			else if (dx < 0)
				return Direction.WEST;
			
			if (dz > 0)
				return Direction.SOUTH;
			else if (dz < 0)
				return Direction.WEST;

			return Direction.NORTH;
		}

		public Direction GetDirectionFrom(LevelNode node)
		{
			(int x, int z) from = node.Position;

			int dx = mPosition.x - from.x;
			int dz = mPosition.z - from.z;

			if (dx > 0)
				return Direction.EAST;
			else if (dx < 0)
				return Direction.WEST;
			
			if (dz > 0)
				return Direction.SOUTH;
			else if (dz < 0)
				return Direction.WEST;

			return Direction.NORTH;
		}

		public static (int x, int z) GetVectorFrom(Direction dir)
		{
			switch(dir)
			{
				case Direction.NORTH:
					return (0, -1);
				case Direction.EAST:
					return (1, 0);
				case Direction.SOUTH:
					return (0, 1);
				case Direction.WEST:
					return (-1, 0);
			}

			return (1, 0);
		}
		

		public static (int x, int z) GetVectorTo(Direction dir)
		{
			switch(dir)
			{
				case Direction.NORTH:
					return (0, 1);
				case Direction.EAST:
					return (-1, 0);
				case Direction.SOUTH:
					return (0, -1);
				case Direction.WEST:
					return (1, 0);
			}

			return (1, 0);
		}

		/// <summary>
		/// All the directions that this node is open to.
		/// </summary>
		/// <returns></returns>
		public List<Direction> GetConnectedNodes()
		{
			return mConnections;
		}

	}
	[Export]
	public NodePath PlayerPath;

	[Export]
	/// <summary>
	/// The radius of chunks around the player to generate.
	/// </summary>
	public int Radius = 5;

	[Export]
	/// <summary>
	/// The size of a chunk.
	/// </summary>
	public float ChunkSize = 20f;

	[Export]
	private string mFloorPath = "res://Scenes/Level0/Floor.tscn";
	[Export]
	private string mCeilingPath = "res://Scenes/Level0/Ceiling.tscn";
	[Export]
	private string mWallPath = "res://Scenes/Level0/Wall.tscn";

	[Export]
	private string mArmiePath = "res://Game/Entities/Armie.tscn";

	private Player mPlayer;
	private OpenSimplexNoise mNoise;
	private Node mChunkGroup;
	private NavigationMeshInstance mNavMeshInst;
	private RandomNumberGenerator mRNG;
	private PauseHandler mPauser;
	private Timer mTimer;

	// used to build a chunk
	// TODO: Create more elegant way of building chunks
	private PackedScene mFloorScene, mCeilingScene, mWallScene, mChunkScene, mArmieScene;

	// reference to generated chunks for easy unloading and loading
	private Dictionary<(int x, int z), Chunk> mChunks = new Dictionary<(int x, int z), Chunk>();

	// references to what type of chunks were in the previous areas
	private Dictionary<(int x, int z), LevelNode> mData = new Dictionary<(int x, int z), LevelNode>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		mPauser = GetNode<PauseHandler>("PauseHandler");
		mChunkGroup = GetNode("Chunks");

		Chunk c = mChunkGroup.GetNode<Chunk>("Chunk");
		if (c != null)
			c.QueueFree();
		mFloorScene = GD.Load<PackedScene>(mFloorPath);
		mWallScene = GD.Load<PackedScene>(mWallPath);
		mCeilingScene = GD.Load<PackedScene>(mCeilingPath);
		mArmieScene = GD.Load<PackedScene>(mArmiePath);
		mChunkScene = GD.Load<PackedScene>("res://Game/WorldGeneration/Chunk.tscn");

		mTimer = GetNode<Timer>("Timer");
		mTimer.Start();
		mTimer.Connect("timeout", this, "Check");
		mPlayer = GetNode<Player>(PlayerPath);
		GD.Print(mPlayer);
		mNoise = new OpenSimplexNoise();
		mNoise.Period = 1.0f;
		mRNG = new RandomNumberGenerator();
		mRNG.Randomize();

		Check();
	}

	public override void _Process(float dt)
	{
		// player wins
		if (mPlayer.Transform.origin.y < -2f)
		{
			// reset the game
			PackedScene scene = GD.Load<PackedScene>("res://Scenes/Level0/Level0.tscn");
			Level1 newLevel = scene.InstanceOrNull<Level1>();
			newLevel.Name = "World";
			Name = "ToRemove";
			Viewport p = GetTree().Root;
			p.RemoveChild(this);
			p.AddChild(newLevel);
			QueueFree();
			
			return;
		}
	}

	private void UpdateGraphData(int originX, int originZ)
	{
		bool isFinished = false;

		while (!isFinished)
		{
			bool isOrigin = true;
			int numNonexplored = 0;
			int closestManhattan = 0;
			LevelNode closestNonexplored = null;
			// find the closest non-explored node
			for (int x = originX - 2 * Radius; x <= originX + 2 * Radius; x++)
			{
				for (int z = originZ - 2 * Radius; z <= originZ + 2 * Radius; z++)
				{
					if (!mData.ContainsKey((x, z)))
						continue;
					LevelNode potential = mData[(x, z)];
					isOrigin = false;
					if (potential.IsExplored)
						continue;
					numNonexplored++;
					if (closestNonexplored == null)
					{
						closestManhattan = Mathf.Abs(originX - x) + Mathf.Abs(originZ - z);
						closestNonexplored = potential;
						continue;
					}
					int manhattan = Mathf.Abs(x - closestNonexplored.Position.x) + Mathf.Abs(z - closestNonexplored.Position.z);
					if (manhattan < closestManhattan)
					{
						closestNonexplored = potential;
						closestManhattan = manhattan;
					}
				}
			}
			
			// not a new graph and there are no more non-explored nodes within range
			if (numNonexplored == 0 && !isOrigin)
			{
				isFinished = true;
				continue;
			}
			// if none exist, then assume we're creating a new graph with an origin node at the player position
			if (closestNonexplored == null)
			{
				closestNonexplored = new LevelNode((originX, originZ));
			}
			if (closestNonexplored.IsExplored)
				GD.Print("is Explored");
			_generateChunkData(originX, originZ, closestNonexplored);
		}
		
	}

	private void _generateChunkData(int originX, int originZ, LevelNode startingNode)
	{
		Queue<LevelNode> f = new Queue<LevelNode>();
		f.Enqueue(startingNode);

		while (f.Count > 0)
		{
			LevelNode v = f.Dequeue();

			//GD.Print($"Generating node ({v.Position.x} {v.Position.z})");

			// this node is already generated, don't do anything further
			int dx = Mathf.Abs(v.Position.x - originX);
			int dz = Mathf.Abs(v.Position.z - originZ);
			if (v.IsExplored)
			{
				//GD.Print($"Node ({v.Position.x} {v.Position.z}) is already explored, skipping...");
				continue;
			}
			else if (dx > 2 * Radius + 1 || dz > 2 * Radius + 1)
			{
				//GD.Print($"Node ({v.Position.x} {v.Position.z}) is too far away from the player, skipping...");
				continue;
			}

			if (!mData.ContainsKey(v.Position))
				mData.Add(v.Position, v);
			
			// add the edge back towards this node's parent
			int numEdges = 0;
			if (v.Parent != null && mRNG.Randf() > 0.3f)
			{
				LevelNode.Direction dir = v.GetDirectionTo(v.Parent);
				v.AddDirection(dir);
				numEdges++;
			}

			for (int i = 0; i < (int)LevelNode.Direction.MAX; i++)
			{
				LevelNode.Direction dir = LevelNode.Direction.MAX;
				(int x, int z) pos = LevelNode.GetVectorFrom((LevelNode.Direction)i);
				if (mRNG.Randf() > 0.1f)
				{
					if (mData.ContainsKey((pos.x + v.Position.x, pos.z + v.Position.z)))
					{
						if (v.AddDirection(dir))
							numEdges++;
					}
				}
			}

			int minEdges = (Mathf.Abs(dx + dz) < 3) ? 3 : 2;

			// generate edges in random directions
			while (numEdges <= minEdges) // include more than 1 edge in it. No dead ends (maybe add some later)
			{
				if (mRNG.RandiRange(0, 1) == 0) // north
				{
					if (v.AddDirection(LevelNode.Direction.NORTH))
						numEdges++;
				}
				if (mRNG.RandiRange(0, 1) == 0) // east
				{
					if (v.AddDirection(LevelNode.Direction.EAST))
						numEdges++;
				}
				if (mRNG.RandiRange(0, 1) == 0) // south
				{
					if (v.AddDirection(LevelNode.Direction.SOUTH))
						numEdges++;
				}
				if (mRNG.RandiRange(0, 1) == 0) // west
				{
					if (v.AddDirection(LevelNode.Direction.WEST))
						numEdges++;
				}
			}
			//GD.Print($"Node ({v.Position.x} {v.Position.z}) has {numEdges} of edges");
			// mark as explored
			v.IsExplored = true;

			// add all nodes into F
			List<LevelNode.Direction> connected = v.GetConnectedNodes();
			foreach (LevelNode.Direction direction in connected)
			{
				(int x, int z) vec = LevelNode.GetVectorFrom(direction);
				(int x, int z) neighborPosition = (vec.x + v.Position.x, vec.z + v.Position.z);
				if (mData.ContainsKey(neighborPosition))
				{
					LevelNode toQueue = mData[neighborPosition];
					if (!toQueue.IsExplored)
						f.Enqueue(toQueue);
				}
				else
				{
					LevelNode neighbor = new LevelNode(neighborPosition, v);
					f.Enqueue(neighbor);
					mData.Add(neighborPosition, neighbor);
				}
			}
		}

	}

	private void Check()
	{

		int chunkX = (int)((mPlayer.Transform.origin.x - ChunkSize / 2) / ChunkSize);
		int chunkZ = (int)((mPlayer.Transform.origin.z - ChunkSize / 2) / ChunkSize);
		UpdateGraphData(chunkX, chunkZ);
		// remove chunks outside the player range
		List<(int x, int z)> toRemove = new List<(int x, int z)>();
		foreach ((int x, int z) key in mChunks.Keys)
		{
			if (key.x > chunkX + Radius || key.x < chunkX - Radius 
				|| key.z > chunkZ + Radius || key.z < chunkZ - Radius)
			{
				toRemove.Add(key);
			}
		}

		foreach((int x, int z) key in toRemove)
		{
			mChunkGroup.RemoveChild(mChunks[key]);
			mChunks.Remove(key);
		}

		// generate new chunks within the player range
		for (int cx = chunkX - Radius; cx <= chunkX + Radius; cx++)
		{    
			for (int cz = chunkZ - Radius; cz <= chunkZ + Radius; cz++)
			{
				(int x, int z) key = (cx, cz);
				if (!mChunks.ContainsKey(key))
				{
					// generate a chunk at this location
					Chunk chunk = GenerateChunkAt(cx, cz);

					mChunkGroup.AddChild(chunk);
					Transform chunkTransform  = chunk.Transform;
					chunkTransform.origin = new Vector3(cx * ChunkSize, 0, cz * ChunkSize);
					chunk.Transform = chunkTransform;
					mChunks.Add(key, chunk);
					if (mRNG.Randf() > 0.995 && Mathf.Abs(cx + cz) > 5)
					{
						Armie arm = mArmieScene.Instance<Armie>();
						AddChild(arm);
						arm.Translation = chunk.Translation + new Vector3(0, 5, -4);
						//arm.Rotation = new Vector3(0, mRNG.RandfRange(-Mathf.Pi, Mathf.Pi), 0);
					}
				}
			}
		}
	}

	private const float NOISE_COORD_MULT = 10;

	/// <summary>
	/// Generates a chunk for the given coordinates.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="z"></param>
	private Chunk GenerateChunkAt(int x, int z)
	{
		Chunk chunk = mChunkScene.Instance<Chunk>();
		chunk.Name = $"({x},{z})";
		if (mRNG.Randf() < 0.985f || Math.Abs(x + z) < 10)
		{
			StaticBody floor = mFloorScene.Instance<StaticBody>();
			chunk.AddChild(floor);
		}
		else // generate an exit
		{

		}
		StaticBody ceiling = mCeilingScene.Instance<StaticBody>();
		if (mRNG.Randf() > 0.5f)
			RemoveCeilingLight(ref ceiling);
		
		chunk.AddChild(ceiling);
		
		if (!mData.ContainsKey((x, z)) || !mData[(x, z)].IsExplored)
			return chunk;
		
		LevelNode chunkData = mData[(x, z)];
		List<LevelNode.Direction> directions = chunkData.GetConnectedNodes();

		// -z -> north
		// +x -> east
		// +z -> south
		// -x -> west
		if (!directions.Contains(LevelNode.Direction.EAST))
		{
			// east
			StaticBody wall = mWallScene.Instance<StaticBody>();
			ApplyTransformNoise(ref wall);
			chunk.AddChild(wall);
		}
		if (!directions.Contains(LevelNode.Direction.WEST))
		{
			
			// west
			StaticBody wall = mWallScene.Instance<StaticBody>();
			wall.Translation = new Vector3(-wall.Translation.x, wall.Translation.y, wall.Translation.z);
			ApplyTransformNoise(ref wall);
			chunk.AddChild(wall);
		}
		// north
		if (!directions.Contains(LevelNode.Direction.NORTH))
		{
			StaticBody wall = mWallScene.Instance<StaticBody>();
			wall.Rotation = new Vector3(0, Mathf.Pi / 2f, 0f);
			wall.Translation = new Vector3(wall.Translation.z, wall.Translation.y, -wall.Translation.x);
			ApplyTransformNoise(ref wall);
			chunk.AddChild(wall);
		}
		// south
		if (!directions.Contains(LevelNode.Direction.SOUTH))
		{
			StaticBody wall = mWallScene.Instance<StaticBody>();
			wall.Rotation = new Vector3(0, Mathf.Pi / 2f, 0f);
			wall.Translation = new Vector3(wall.Translation.z, wall.Translation.y, wall.Translation.x);
			ApplyTransformNoise(ref wall);
			chunk.AddChild(wall);
		}

		return chunk;
	}

	private void RemoveCeilingLight(ref StaticBody ceiling)
	{
		MeshInstance ceilLight = ceiling.GetNode<MeshInstance>("CeilingLight");
		if (ceilLight == null)
			return;
		ceiling.RemoveChild(ceilLight);
		ceilLight.QueueFree();
	}

	private void ApplyTransformNoise(ref StaticBody body)
	{
		if (mRNG.Randf() > 0.7f)
		{
			Vector3 noise = Vector3.Zero;
			if (mRNG.Randf() > 0.1f)
				noise.x += mRNG.Randfn(0, 3);
				
			if (mRNG.Randf() > 0.1f)
				noise.z += mRNG.Randfn(0, 3);

			if (mRNG.Randf() > 0.95f)
				noise.y += mRNG.Randfn(0, 0.5f);

			body.Translation += noise;
		}

		if (mRNG.Randf() > 0.7f)
		{
			Vector3 scale = Vector3.Zero;

			if (mRNG.Randf() > 0.5f)
				scale.x += mRNG.Randfn(0, 3);
				
			if (mRNG.Randf() > 0.5f)
				scale.z += mRNG.Randfn(0, 3);
			body.Scale += scale;
		}
	}
}
