using UnityEngine;
using CodeMonkey.Utils;

//This class for creating, changing and debugging grid system of the game,all cells of the grid stores an integer.
//This class can be used for create multiple grids
public class Grid
{
    private int width, height;
    private float cellSize;
    private int[,] gridArray;
    private Vector3 originPosition;
    
    //Debug Settings
    private int fontSize = 25;
    private Color debugColor = Color.black;
    private TextAnchor anchorLoc = TextAnchor.MiddleCenter;
    private TextMesh[,] debugTextArray;
    
    public Grid(int width, int height,float cellSize,Vector3 originPosition)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;
        
        
        debugTextArray = new TextMesh[width, height];
        CreateGrid();
    }
    
    //Creates the grid with given parameters
    private void CreateGrid()
    {
        gridArray = new int[width, height];

        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                debugTextArray[x,z] = UtilsClass.CreateWorldText(gridArray[x, z].ToString(), GameObject.Find("Grid").transform, GetWorldPosition(x, z), fontSize ,debugColor,anchorLoc);
                
                Debug.DrawLine(GetWorldPosition(x,z),GetWorldPosition(x,z+1),debugColor,3600f);
                Debug.DrawLine(GetWorldPosition(x,z),GetWorldPosition(x+1,z),debugColor,3600f);
            }
            Debug.DrawLine(GetWorldPosition(0,height),GetWorldPosition(width,height));
            Debug.DrawLine(GetWorldPosition(width,0),GetWorldPosition(width,height));
        }
    }
    
    //Returns world position of the grid cell
    private Vector3 GetWorldPosition(int x, int z)
    {
        return new Vector3(x,0,z) * cellSize + originPosition;
    }
    private void GetXY(Vector3 worldPosition, out int x, out int z)
    {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        z = Mathf.FloorToInt((worldPosition - originPosition).z/ cellSize);
    }
    public void SetValue(int x, int z, int value)
    {
        if (x >= 0 && z >= 0 && x < width && z < height)
        {
            gridArray[x, z] = value;
            debugTextArray[x, z].text = gridArray[x, z].ToString();
        }
    }
    public void SetValue(Vector3 worldPosition, int value)
    {
        int x, z;
        GetXY(worldPosition, out x,out z);
        SetValue(x,z,value);
    }

    public int GetValue(int x, int z)
    {
        if (x >= 0 && z >= 0 && x < width && z < height)
        {
            return gridArray[x, z];
        }
        else
        {
            return 0;
        }
    }
}
