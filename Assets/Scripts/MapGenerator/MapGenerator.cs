using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

public class MapGenerator : MonoBehaviour
{
    public List<Room> rooms;
    public Hallway vertical_hallway;
    public Hallway horizontal_hallway;
    public Room start;
    public Room target;

    // Constraint: How big should the dungeon be at most
    // this will limit the run time (~10 is a good value 
    // during development, later you'll want to set it to 
    // something a bit higher, like 25-30)
    public int MAX_SIZE;

    // set this to a high value when the generator works
    // for debugging it can be helpful to test with few rooms
    // and, say, a threshold of 100 iterations
    public int THRESHOLD;

    // keep the instantiated rooms and hallways here 
    private List<GameObject> generated_objects;
    
    int iterations;

    public void Generate()
    {
        // dispose of game objects from previous generation process
        foreach (var go in generated_objects)
        {
            Destroy(go);
        }
        generated_objects.Clear();
        
        generated_objects.Add(start.Place(new Vector2Int(0,0)));
        List<Door> doors = start.GetDoors();
        List<Vector2Int> occupied = new List<Vector2Int>();
        occupied.Add(new Vector2Int(0, 0));
        iterations = 0;
        GenerateWithBacktracking(occupied, doors, 1);
    }


    bool GenerateWithBacktracking(List<Vector2Int> occupied, List<Door> doors, int depth)
    {
        if (iterations > THRESHOLD) throw new System.Exception("Iteration limit exceeded");

        // If there are no more doors that need to be connected check 
        // if the dungeon has the required minimum size and 
        // return true if it does, false otherwise
        if (doors.Count == 0)
        {
            return depth >= 5;
        }

        List<Door> potentialDoors = new List<Door>(doors);

        while (true)
        {   
            // Select one of the doors that still have to be connected
            Door targetDoor = potentialDoors[Random.Range(0, potentialDoors.Count)];
            if (occupied.Contains(targetDoor.GetMatching().GetGridCoordinates()))
            {
                potentialDoors.Remove(targetDoor);
                continue;
            }

            // Determines which of the available rooms are compatible with this door
            // if there are none, return false
            List<Room> validRooms = new List<Room>();
            foreach (Room room in rooms)
            {
                foreach (Door door in room.GetDoors())
                {
                    iterations++;
                    if (targetDoor.IsMatching(door))
                    {
                        validRooms.Add(room);
                        break;
                    }
                }
            }
            if (validRooms.Count == 0)
            {
                return false;
            }

            while (true)
            {
                Room newRoom = validRooms[0];
                if (newRoom == null)
                {
                    return false;
                }


                if (potentialDoors.Count == 0)
                {
                    return false;
                }
            }
        }
    

        // Tentatively place the room and recursively call GenerateWithBacktracking
        Vector2Int offset = targetDoor.GetMatching().GetGridCoordinates();
        occupied.AddRange(newRoom.GetGridCoordinates(offset));
        doors.AddRange(newRoom.GetDoors(offset));
        doors.Remove(targetDoor);
        doors.Remove(targetDoor.GetMatching());
        if (GenerateWithBacktracking(occupied, doors, depth + 1))
        {
            //Instantiate prefab (place room)
            newRoom.Place(newRoom.GetGridCoordinates(offset)[0]);
            return true;
        }

        return false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        generated_objects = new List<GameObject>();
        Generate();
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.gKey.wasPressedThisFrame)
            Generate();
    }
}
