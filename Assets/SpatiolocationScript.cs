using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class SpatiolocationScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;
	
	public AudioClip[] SFX;
	
	public KMSelectable[] Arrows;
	public KMSelectable Forward, Analyze;
	public Material White;
	public Material[] SpiralingBackground;
	public MeshRenderer Background;
	public GameObject AllTheButtons;
	
	int[] Coordinates = new int[4], Rotations = new int[3], Key = new int[4], Exit = new int[4];
	
	string[,,,] MazeGenerated = new string[8,8,8,8];
	string[,,,] ConnectionsGenerated = new string[8,8,8,8];
	
	static int Symmetry = 3;
	int MazeRow = Symmetry, MazeColumn = Symmetry, MazeFloor = Symmetry, MazeTimeline = Symmetry; 
	int ForcedSteps = 50000; string CurrentOrientation = "-X"; bool HoldingKey = false;
	Coroutine HoldCheckCoroutine;
	int Loop = 0;
	
	// Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool ModuleSolved;
	
	void Awake()
    {
        moduleId = moduleIdCounter++;
		for (int a = 0; a < Arrows.Count(); a++)
        {
			int Movement = a;
            Arrows[Movement].OnInteract += delegate
            {
                ArrowRotation(Movement);
                return false;
            };
		}
		Forward.OnInteract += delegate () { MoveForward(); return false; };
        Analyze.OnInteract += delegate () { CheckForAnalysis(); return false; };
		Analyze.OnInteractEnded += delegate () {CenterRelease(); };
	}

	void Start()
	{
		StartCoroutine(LoopingBackground());
		GenerateMazeOriginShift(MazeRow, MazeColumn, MazeFloor, MazeTimeline);
		CoordinateGeneration();
	}
	
	void GenerateMazeOriginShift(int mazerow, int mazecolumn, int mazefloor, int mazetimeline)
	{
		if (mazerow > 0 && mazecolumn > 0  && mazefloor > 0 && mazetimeline > 0 && (mazerow + mazecolumn + mazefloor + mazetimeline > 4))
		{
			MazeGenerated = new string[mazetimeline,mazefloor,mazecolumn,mazerow];
			ConnectionsGenerated = new string[mazetimeline,mazefloor,mazecolumn,mazerow];
			for (int w = 0; w < mazetimeline; w++)
			{
				for (int z = 0; z < mazefloor; z++)
				{
					for (int x = 0; x < mazecolumn; x++)
					{
						for (int y = 0; y < mazerow; y++)
						{
							//U = Up, D = Down, L = Left, R = Right, T = Top, B = Bottom, F = Future, P = Past
							MazeGenerated[w,z,x,y] = "UDLRTBFP";
							if (y < mazerow - 1)
							{
								ConnectionsGenerated[w,z,x,y] = "R";
							}
							
							else
							{
								if (x < mazecolumn - 1)
								{
									ConnectionsGenerated[w,z,x,y] = "D";
								}
								
								else
								{
									if (z < mazefloor - 1)
									{
										ConnectionsGenerated[w,z,x,y] = "B";
									}
									
									else
									{
										if (w < mazetimeline - 1)
										{
											ConnectionsGenerated[w,z,x,y] = "P";
										}
										
										else
										{
											ConnectionsGenerated[w,z,x,y] = "X";
										}
									}
								}
							}
							
						}
					}
				}
			}
			
			int CurrentRow = mazerow - 1, CurrentColumn = mazecolumn - 1, CurrentFloor = mazefloor - 1, CurrentTimeline = mazetimeline - 1;
			for (int x = 0; x < ForcedSteps; x++)
			{
				bool StepValid = false;
				do
				{
					int Movement = UnityEngine.Random.Range(0,8);
					switch(Movement)
					{
						case 0:
							if (CurrentColumn != 0)
							{
								ConnectionsGenerated[CurrentTimeline,CurrentFloor,CurrentColumn,CurrentRow] = "U";
								CurrentColumn = ((CurrentColumn - 1) + mazecolumn) % mazecolumn;
								ConnectionsGenerated[CurrentTimeline,CurrentFloor,CurrentColumn,CurrentRow] = "X";
								StepValid = true;
							}
							break;
						case 1:
							if (CurrentColumn != mazecolumn - 1)
							{
								ConnectionsGenerated[CurrentTimeline,CurrentFloor, CurrentColumn,CurrentRow] = "D";
								CurrentColumn = (CurrentColumn + 1) % mazecolumn;
								ConnectionsGenerated[CurrentTimeline,CurrentFloor, CurrentColumn,CurrentRow] = "X";
								StepValid = true;
							}
							
							break;
						case 2:
							if (CurrentRow != 0)
							{
								ConnectionsGenerated[CurrentTimeline,CurrentFloor, CurrentColumn,CurrentRow] = "L";
								CurrentRow = ((CurrentRow - 1) + mazerow) % mazerow;
								ConnectionsGenerated[CurrentTimeline,CurrentFloor, CurrentColumn,CurrentRow] = "X";
								StepValid = true;
							}
							break;
						case 3:
							if (CurrentRow != mazerow - 1)
							{
								ConnectionsGenerated[CurrentTimeline,CurrentFloor, CurrentColumn,CurrentRow] = "R";
								CurrentRow = (CurrentRow + 1) % mazerow;
								ConnectionsGenerated[CurrentTimeline,CurrentFloor, CurrentColumn,CurrentRow] = "X";
								StepValid = true;
							}
							break;
						case 4:
							if (CurrentFloor != 0)
							{
								ConnectionsGenerated[CurrentTimeline,CurrentFloor, CurrentColumn,CurrentRow] = "T";
								CurrentFloor = ((CurrentFloor - 1) + mazefloor) % mazefloor;
								ConnectionsGenerated[CurrentTimeline,CurrentFloor, CurrentColumn,CurrentRow] = "X";
								StepValid = true;
							}
							break;
						case 5:
							if (CurrentFloor != mazefloor - 1)
							{
								ConnectionsGenerated[CurrentTimeline,CurrentFloor, CurrentColumn,CurrentRow] = "B";
								CurrentFloor = (CurrentFloor + 1) % mazefloor;
								ConnectionsGenerated[CurrentTimeline,CurrentFloor, CurrentColumn,CurrentRow] = "X";
								StepValid = true;
							}
							break;
						case 6:
							if (CurrentTimeline != 0)
							{
								ConnectionsGenerated[CurrentTimeline,CurrentFloor, CurrentColumn,CurrentRow] = "F";
								CurrentTimeline = ((CurrentTimeline - 1) + mazetimeline) % mazetimeline;
								ConnectionsGenerated[CurrentTimeline,CurrentFloor, CurrentColumn,CurrentRow] = "X";
								StepValid = true;
							}
							break;
						case 7:
							if (CurrentTimeline != mazetimeline - 1)
							{
								ConnectionsGenerated[CurrentTimeline,CurrentFloor, CurrentColumn,CurrentRow] = "P";
								CurrentTimeline = (CurrentTimeline + 1) % mazetimeline;
								ConnectionsGenerated[CurrentTimeline,CurrentFloor, CurrentColumn,CurrentRow] = "X";
								StepValid = true;
							}
							break;
						default:
							break;
					}
				}
				while (!StepValid);
			}
			
			for (int w = 0; w < mazetimeline; w++)
			{
				for (int z = 0; z < mazefloor; z++)
				{
					for (int x = 0; x < mazecolumn; x++)
					{
						for (int y = 0; y < mazerow; y++)
						{
							switch(ConnectionsGenerated[w,z,x,y])
							{
								case "U":
									MazeGenerated[w,z,x,y] = MazeGenerated[w,z,x,y].Replace("U", "");
									MazeGenerated[w,z,((x-1)+mazecolumn)%mazecolumn,y] = MazeGenerated[w,z,((x-1)+mazecolumn)%mazecolumn,y].Replace("D", "");
									break;
								case "D":
									MazeGenerated[w,z,x,y] = MazeGenerated[w,z,x,y].Replace("D", "");
									MazeGenerated[w,z,(x+1)%mazecolumn,y] = MazeGenerated[w,z,(x+1)%mazecolumn,y].Replace("U", "");
									break;
								case "L":
									MazeGenerated[w,z,x,y] = MazeGenerated[w,z,x,y].Replace("L", "");
									MazeGenerated[w,z,x,((y-1)+mazerow)%mazerow] = MazeGenerated[w,z,x,((y-1)+mazerow)%mazerow].Replace("R", "");
									break;									
								case "R":
									MazeGenerated[w,z,x,y] = MazeGenerated[w,z,x,y].Replace("R", "");
									MazeGenerated[w,z,x,(y+1)%mazerow] = MazeGenerated[w,z,x,(y+1)%mazerow].Replace("L", "");
									break;
								case "T":
									MazeGenerated[w,z,x,y] = MazeGenerated[w,z,x,y].Replace("T", "");
									MazeGenerated[w,((z-1)+mazefloor)%mazefloor,x,y] = MazeGenerated[w,((z-1)+mazefloor)%mazefloor,x,y].Replace("B", "");
									break;
								case "B":
									MazeGenerated[w,z,x,y] = MazeGenerated[w,z,x,y].Replace("B", "");
									MazeGenerated[w,(z+1)%mazefloor,x,y] = MazeGenerated[w,(z+1)%mazefloor,x,y].Replace("T", "");
									break;
								case "F":
									MazeGenerated[w,z,x,y] = MazeGenerated[w,z,x,y].Replace("F", "");
									MazeGenerated[((w-1)+mazetimeline)%mazetimeline,z,x,y] = MazeGenerated[((w-1)+mazetimeline)%mazetimeline,z,x,y].Replace("P", "");
									break;
								case "P":
									MazeGenerated[w,z,x,y] = MazeGenerated[w,z,x,y].Replace("P", "");
									MazeGenerated[(w+1)%mazetimeline,z,x,y] = MazeGenerated[(w+1)%mazetimeline,z,x,y].Replace("F", "");
									break;
								default:
									break;
							}
						}
					}
				}
			}
			
			Debug.LogFormat("[Spatiolocation #{0}] ----------------------------------------------------", moduleId);
			Debug.LogFormat("[Spatiolocation #{0}] < Maze Walls Generated by the Module >", moduleId);
			for (int w = 0; w < mazetimeline; w++)
			{
				if (mazetimeline != 1)
				{
					Debug.LogFormat("[Spatiolocation #{0}] Timeline {1}:", moduleId, w.ToString());
				}
				for (int z = 0; z < mazefloor; z++)
				{
					if (mazefloor != 1)
					{
						Debug.LogFormat("[Spatiolocation #{0}] Floor {1}:", moduleId, z.ToString());
					}
					for (int x = 0; x < mazecolumn; x++)
					{
						string Mazery = "";
						for (int y = 0; y < mazerow; y++)
						{
							Mazery += "[" + MazeGenerated[w,z,x,y] + "]";
						}
						Debug.LogFormat("[Spatiolocation #{0}] {1}", moduleId, Mazery);
					}
					Debug.LogFormat("[Spatiolocation #{0}] ----------------------------------------------------", moduleId);
				}
			}
			
		}
	}
	
	void CoordinateGeneration()
	{
		for (int f = 0; f < 3; f++)
		{
			Rotations[f] = UnityEngine.Random.Range(0,4);
		}
		Coordinates = new int[4] {0,0,0,0};
		Redo:
		int[,] GivenImportantCoordinates = new int[2,4];
		for (int q = 0; q < 4; q++)
		{
			Coordinates[q] = UnityEngine.Random.Range(0,Symmetry);
		}
		for (int x = 0; x < 2; x++)
		{
			for (int y = 0; y < 4; y++)
			{
				GivenImportantCoordinates[x,y] = UnityEngine.Random.Range(0,Symmetry);
			}
			
			if (x != 0)
			{
				for (int z = 0; z < x; z++)
				{
					int ManhattanDistance = 0, Placement = 0, PlacementTwo = 0;
					for (int y = 0; y < 4; y++)
					{
						ManhattanDistance = GivenImportantCoordinates[x,y] - GivenImportantCoordinates[z,y] < 0 ? ManhattanDistance + ((GivenImportantCoordinates[x,y] - GivenImportantCoordinates[z,y]) * -1) : ManhattanDistance + (GivenImportantCoordinates[x,y] - GivenImportantCoordinates[z,y]);
						Placement += GivenImportantCoordinates[x,y];
						PlacementTwo += GivenImportantCoordinates[z,y];
					}
					
					if (ManhattanDistance < ((MazeRow+MazeColumn+MazeFloor+MazeTimeline)-4)/2)
					{
						goto Redo;
					}
				}
			}
		}
		
		CurrentOrientation = GiveOrientation(Rotations);
		for (int x = 0; x < 4; x++)
		{
			Key[x] = GivenImportantCoordinates[0,x];
			Exit[x] = GivenImportantCoordinates[1,x];
		}
		
		Debug.LogFormat("[Spatiolocation #{0}] < Location Coordinates + Other Info >", moduleId);
		Debug.LogFormat("[Spatiolocation #{0}] Key Location: ({1},{2},{3},{4})", moduleId, GivenImportantCoordinates[0,0], GivenImportantCoordinates[0,1], GivenImportantCoordinates[0,2], GivenImportantCoordinates[0,3]);
		Debug.LogFormat("[Spatiolocation #{0}] Exit Location: ({1},{2},{3},{4})", moduleId, GivenImportantCoordinates[1,0], GivenImportantCoordinates[1,1], GivenImportantCoordinates[1,2], GivenImportantCoordinates[1,3]);
		Debug.LogFormat("[Spatiolocation #{0}] ----------------------------------------------------", moduleId);
		Debug.LogFormat("[Spatiolocation #{0}] ----------------------------------------------------", moduleId);
		Debug.LogFormat("[Spatiolocation #{0}] Current Location: ({1},{2},{3},{4}) + Current Orientation: {5}{6}{7} ({8})", moduleId, Coordinates[0], Coordinates[1], Coordinates[2], Coordinates[3], Rotations[0], Rotations[1], Rotations[2], CurrentOrientation);
	}
	
	void ArrowRotation(int Number)
	{
		if (ModuleSolved)
			return;
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        Arrows[Number].AddInteractionPunch(0.5f);
		string Moved = "";
		switch (Number)
		{
			case 0:
				Moved = "left";
				Rotations[2] = ((Rotations[2]-1)+4)%4;
				break;
			case 1:
				Moved = "right";
				Rotations[2] = (Rotations[2]+1)%4;
				break;
			case 2:
				Moved = "up";
				Rotations[1] = ((Rotations[1]-1)+4)%4;
				break;
			case 3:
				Moved = "down";
				Rotations[1] = (Rotations[1]+1)%4;
				break;
			case 4:
				Moved = "towards ana";
				Rotations[0] = ((Rotations[0]-1)+4)%4;
				break;
			case 5:
				Moved = "towards cata";
				Rotations[0] = (Rotations[0]+1)%4;
				break;
			default:
				break;
		}
		CurrentOrientation = GiveOrientation(Rotations);
		Debug.LogFormat("[Spatiolocation #{0}] You rotated {1} relative to original orientation (000 [-X]). Current Location: ({2},{3},{4},{5}) + Current Orientation: {6}{7}{8} ({9})", moduleId, Moved, Coordinates[0], Coordinates[1], Coordinates[2], Coordinates[3], Rotations[0], Rotations[1], Rotations[2], CurrentOrientation);
	}
	
	void MoveForward()
	{
		if (ModuleSolved)
			return;
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		Forward.AddInteractionPunch(0.5f);
		string PossibleWalls = "UDLRTBFP";
		string[] Match = {"-X", "+X", "-Y", "+Y", "-Z", "+Z", "-W", "+W"};
		if (MazeGenerated[Coordinates[0],Coordinates[1],Coordinates[2],Coordinates[3]].ToCharArray().Count(c => c == PossibleWalls[Array.IndexOf(Match,CurrentOrientation)]) == 0)
		{
			switch(PossibleWalls[Array.IndexOf(Match,CurrentOrientation)].ToString())
			{
				case "U":
					Coordinates[2] = ((Coordinates[2] - 1) + MazeColumn) % MazeColumn;
					break;
				case "D":
					Coordinates[2] = (Coordinates[2] + 1) % MazeColumn;
					break;
				case "L":
					Coordinates[3] = ((Coordinates[3] - 1) + MazeRow) % MazeRow;
					break;
				case "R":
					Coordinates[3] = (Coordinates[3] + 1) % MazeRow;
					break;
				case "T":
					Coordinates[1] = ((Coordinates[1] - 1) + MazeFloor) % MazeFloor;
					break;
				case "B":
					Coordinates[1] = (Coordinates[1] + 1) % MazeFloor;
					break;
				case "F":
					Coordinates[0] = ((Coordinates[0] - 1) + MazeTimeline) % MazeTimeline;
					break;
				case "P":
					Coordinates[0] = (Coordinates[0] + 1) % MazeTimeline;
					break;
				default:
					break;
			}
			Debug.LogFormat("[Spatiolocation #{0}] You move towards that direction successfully. Current Location: ({1},{2},{3},{4}) + Current Orientation: {5}{6}{7} ({8})", moduleId, Coordinates[0], Coordinates[1], Coordinates[2], Coordinates[3], Rotations[0], Rotations[1], Rotations[2], CurrentOrientation);
		}
		
		else
		{
			Module.HandleStrike();
			Debug.LogFormat("[Spatiolocation #{0}] You hit a wall while trying to move towards that direction. Current Location: ({1},{2},{3},{4}) + Current Orientation: {5}{6}{7} ({8})", moduleId, Coordinates[0], Coordinates[1], Coordinates[2], Coordinates[3], Rotations[0], Rotations[1], Rotations[2], CurrentOrientation);
		}
	}
	
	void CheckForAnalysis()
	{
		if (ModuleSolved)
			return;
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        Analyze.AddInteractionPunch(0.5f);
		StartCoroutine(PlayNoises());
		if (HoldCheckCoroutine != null)
            StopCoroutine(HoldCheckCoroutine);
        HoldCheckCoroutine = StartCoroutine(HoldChecker());
	}
	
	IEnumerator HoldChecker()
    {
        yield return new WaitForSecondsRealtime(0.4f);
        CenterHold();
    }
	
	void CenterRelease()
    {
        if (ModuleSolved)
			return;
        if (HoldCheckCoroutine != null)
            StopCoroutine(HoldCheckCoroutine);
    }
	
	IEnumerator PlayNoises()
    {
		Debug.LogFormat("[Spatiolocation #{0}] You played your spatial sonar in the given orientation.", moduleId);
		int[] CurrentLocation = new int[4];
		for (int i = 0; i < 4; i++)
		{
			CurrentLocation[i] = Coordinates[i];
		}
		
		int Alternate = 0; bool Analyzing = true;
		while (Analyzing)
		{
			if (Alternate%2 == 0)
			{
				if (CurrentLocation.SequenceEqual(Key) && !HoldingKey)
					Audio.PlaySoundAtTransform(SFX[1].name, transform);
				if (CurrentLocation.SequenceEqual(Exit))
					Audio.PlaySoundAtTransform(SFX[0].name, transform);
			}
			
			else
			{
				string PossibleWalls = "UDLRTBFP";
				string[] Match = {"-X", "+X", "-Y", "+Y", "-Z", "+Z", "-W", "+W"};
				if (MazeGenerated[CurrentLocation[0],CurrentLocation[1],CurrentLocation[2],CurrentLocation[3]].ToCharArray().Count(c => c == PossibleWalls[Array.IndexOf(Match,CurrentOrientation)]) == 0)
				{
					switch(PossibleWalls[Array.IndexOf(Match,CurrentOrientation)].ToString())
					{
						case "U":
							CurrentLocation[2] = ((CurrentLocation[2] - 1) + MazeColumn) % MazeColumn;
							break;
						case "D":
							CurrentLocation[2] = (CurrentLocation[2] + 1) % MazeColumn;
							break;
						case "L":
							CurrentLocation[3] = ((CurrentLocation[3] - 1) + MazeRow) % MazeRow;
							break;
						case "R":
							CurrentLocation[3] = (CurrentLocation[3] + 1) % MazeRow;
							break;
						case "T":
							CurrentLocation[1] = ((CurrentLocation[1] - 1) + MazeFloor) % MazeFloor;
							break;
						case "B":
							CurrentLocation[1] = (CurrentLocation[1] + 1) % MazeFloor;
							break;
						case "F":
							CurrentLocation[0] = ((CurrentLocation[0] - 1) + MazeTimeline) % MazeTimeline;
							break;
						case "P":
							CurrentLocation[0] = (CurrentLocation[0] + 1) % MazeTimeline;
							break;
						default:
							break;
					}
				}
				
				else
				{
					Audio.PlaySoundAtTransform(SFX[2].name, transform);
					yield break;
				}
			}
			Alternate++;
			yield return new WaitForSecondsRealtime(0.5f);
		}
	}
	
	void CenterHold()
	{
		if (Coordinates.SequenceEqual(Key) && !HoldingKey)
		{
			HoldingKey = true;
			Debug.LogFormat("[Spatiolocation #{0}] You collected the key.", moduleId);
			Analyze.AddInteractionPunch(0.5f);
			return;
		}
		
		if (Coordinates.SequenceEqual(Exit))
		{
			if (HoldingKey)
			{
				Module.HandlePass();
				ModuleSolved = true;
				Background.material = White;
				AllTheButtons.SetActive(false);		
				Audio.PlaySoundAtTransform(SFX[3].name, transform);
				Debug.LogFormat("[Spatiolocation #{0}] You are at the exit and you have the key. Module solved!", moduleId);
				return;
			}
			Module.HandleStrike();
            Debug.LogFormat("[Spatiolocation #{0}] You are at the exit but you don't have the key. That is not allowed!", moduleId);
            return;
		}
		Debug.LogFormat("[Spatiolocation #{0}] You are not at the key nor the exit. That is not allowed!", moduleId);
        Module.HandleStrike();
	}
	
	IEnumerator LoopingBackground()
	{
		while (!ModuleSolved)
		{
			Background.material = SpiralingBackground[Loop];
			Loop = (Loop+1)%SpiralingBackground.Length;
			yield return new WaitForSecondsRealtime(0.03f); 
		}
	}
	
	string GiveOrientation(int[] rotationNumbers)
	{
		if (rotationNumbers.Length != 3)
			return "";

		Dictionary<string, string[]> cycles = new Dictionary<string, string[]>()
		{
			{ "XY", new[] {"-X", "+Y", "+X", "-Y"} },
			{ "XZ", new[] {"-X", "-Z", "+X", "+Z"} },
			{ "XW", new[] {"-X", "-W", "+X", "+W"} },
			{ "YZ", new[] {"+Y", "+Z", "-Y", "-Z"} },
			{ "YW", new[] {"+Y", "+W", "-Y", "-W"} },
			{ "ZW", new[] {"+Z", "+W", "-Z", "-W"} }
		};

		string temp = cycles["XW"][rotationNumbers[0] % 4];

		char axis = temp[1];
		if (axis == 'X')
		{
			temp = cycles["XZ"][(Array.IndexOf(cycles["XZ"], temp) + rotationNumbers[1]) % 4];
			axis = temp[1];
			if (axis == 'X')
				temp = cycles["XY"][(Array.IndexOf(cycles["XY"], temp) + rotationNumbers[2]) % 4];
			else if (axis == 'Z')
				temp = cycles["YZ"][(Array.IndexOf(cycles["YZ"], temp) + rotationNumbers[2]) % 4];
		}
		else if (axis == 'W')
		{
			temp = cycles["ZW"][(Array.IndexOf(cycles["ZW"], temp) + rotationNumbers[1]) % 4];
			axis = temp[1];
			if (axis == 'W')
				temp = cycles["YW"][(Array.IndexOf(cycles["YW"], temp) + rotationNumbers[2]) % 4];
			else if (axis == 'Z')
				temp = cycles["YZ"][(Array.IndexOf(cycles["YZ"], temp) + rotationNumbers[2]) % 4];
		}

		return temp;
	}
	
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} u/d/l/r/a/c/m/f [Presses the button(s) in the specified position(s). !{0} m / !{0} f presses the upper center button] | !{0} sonar/s [Uses the spatial sonar in the given orientation] | !{0} hold/h [Holds the lower center button]";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*hold\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(command, @"^\s*h\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            Analyze.OnInteract();
            yield return new WaitForSeconds(1f);
            Analyze.OnInteractEnded();
            yield break;
        }
		
		if (Regex.IsMatch(command, @"^\s*sonar\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(command, @"^\s*s\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            Analyze.OnInteract();
            yield return null;
            Analyze.OnInteractEnded();
            yield break;
        }

        string[] valids = { "l", "r", "u", "d", "a", "c", "m", "f" };
        command = command.Replace(" ", "");
        command = command.ToLower();
        for (int i = 0; i < command.Length; i++)
        {
            if (!valids.Contains(command.ElementAt(i) + ""))
            {
                yield return "sendtochaterror The specified position '" + command.ElementAt(i) + "' for a button is not valid!";
                yield break;
            }
        }
        for (int i = 0; i < command.Length; i++)
        {
            if (command.ElementAt(i) == 'f' || command.ElementAt(i) == 'm')
            {
                yield return null;
                yield return "trycancel The command is cancelled during move #" + (i + 1) + ".";
                if (command.Length > 1) yield return "strikemessage input #" + (i + 1);
                Forward.OnInteract();
                yield return new WaitForSeconds(0.2f);
            }
            else
            {
                yield return null;
                yield return "trycancel The command is cancelled during move #" + (i + 1) + ".";
                if (command.Length > 1) yield return "strikemessage input #" + (i + 1);
                Arrows[Array.IndexOf(valids, command.ElementAt(i) + "") % 5].OnInteract();
                yield return new WaitForSeconds(0.2f);
            }
        }
        yield break;
    }
}