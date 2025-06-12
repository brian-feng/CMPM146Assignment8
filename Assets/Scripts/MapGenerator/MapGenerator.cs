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

    bool targetPlaced = false;

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
        Debug.Log("depth = " + depth);
        Debug.Log("doors = " + doors.Count);
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
            iterations++;
            if (iterations > THRESHOLD) throw new System.Exception("Iteration limit exceeded");
            if (potentialDoors.Count == 0)
            {
                return false;
            }

            // Select one of the doors that still have to be connected
            Door targetDoor = potentialDoors[Random.Range(0, potentialDoors.Count)];
            if (occupied.Contains(targetDoor.GetMatching().GetGridCoordinates()))
            {
                potentialDoors.Remove(targetDoor);
                continue;
            }

            if (targetDoor.GetMatching().GetGridCoordinates().magnitude > 25)
            {
                potentialDoors.Remove(targetDoor);
                continue;
            }
            // Determines which of the available rooms are compatible with this door
            // if there are none, return false
            List<Room> validRooms = new List<Room>();
            foreach (Room room in rooms)
            {
                bool hasMatch = false;
                bool hasBadDoor = false;
                foreach (Door door in room.GetDoors(targetDoor.GetMatching().GetGridCoordinates()))
                {
                    iterations++;
                    if (targetDoor.IsMatching(door))
                    {
                        hasMatch = true;
                    }
                    else if (occupied.Contains(door.GetMatching().GetGridCoordinates())){
                        hasBadDoor = true;
                    }
                }
                if (hasMatch && !hasBadDoor) {
                    validRooms.Add(room);
                }
            }
            if (validRooms.Count == 0)
            {
                potentialDoors.Remove(targetDoor);
                continue;
            }

            while (true)
            {
                if (validRooms.Count == 0)
                {
                    potentialDoors.Remove(targetDoor);
                    break;
                }
                // Tentatively place the room and recursively call GenerateWithBacktracking
                Room newRoom = selectRoom(validRooms);
                Vector2Int offset = targetDoor.GetMatching().GetGridCoordinates();
                occupied.AddRange(newRoom.GetGridCoordinates(offset));

                List<Door> newDoors = newRoom.GetDoors(offset);
                foreach (Door door in newDoors)
                {
                    if (!door.IsMatching(targetDoor))
                    {
                        doors.Add(door);
                    }
                }
                doors.Remove(targetDoor);

                if (GenerateWithBacktracking(occupied, doors, depth + 1))
                {
                    // Instantiate prefab (place room)
                    if (newRoom.name == "Deadend" && !targetPlaced)
                    {
                        target.Place(target.GetGridCoordinates(offset)[0]);
                        targetPlaced = true;
                    }
                    else
                    {
                        newRoom.Place(newRoom.GetGridCoordinates(offset)[0]);
                    }
                    return true;
                }
                else
                {
                    validRooms.Remove(newRoom);
                    continue;
                }
            }
        }
    }

    Room selectRoom(List<Room> rooms)
    {
        int totalWeight = 0;
        foreach (Room room in rooms)
        {
            totalWeight += room.weight;
        }

        int n = Random.Range(0, totalWeight);
        int count = 0;
        foreach (Room room in rooms)
        {
            count += room.weight;
            if (n < count)
            {
                return room;
            }
        }
        return rooms[Random.Range(0, rooms.Count)];
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
