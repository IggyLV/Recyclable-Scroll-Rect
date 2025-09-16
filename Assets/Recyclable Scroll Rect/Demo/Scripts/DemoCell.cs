using UnityEngine;
using UnityEngine.UI;
using PolyAndCode.UI;

//Cell class for demo. A cell in Recyclable Scroll Rect must have a cell class inheriting from ICell.
//The class is required to configure the cell(updating UI elements etc) according to the data during recycling of cells.
//The configuration of a cell is done through the DataSource SetCellData method.
//Check RecyclableScrollerDemo class
public class DemoCell : MonoBehaviour, ICell
{
    //UI
    public Text nameLabel;
    public Text genderLabel;
    public Text idLabel;
    public Text heightLabel;

    //Model
    private ContactInfo _contactInfo;
    private int _cellIndex;

    public int CellIndex
    {
        get => _cellIndex;
        private set => _cellIndex = value;
    }

    private void Start()
    {
        //Can also be done in the inspector
        GetComponent<Button>().onClick.AddListener(ButtonListener);
    }

    //This is called from the SetCell method in DataSource
    public void ConfigureCell(ContactInfo contactInfo, int cellIndex)
    {
        CellIndex = cellIndex;
        _contactInfo = contactInfo;

        nameLabel.text = contactInfo.Name;
        genderLabel.text = contactInfo.Gender;
        idLabel.text = contactInfo.id;
        heightLabel.text = $"{contactInfo.height}px";

        Vector2 sizeDelta = ((RectTransform)transform).sizeDelta;
        ((RectTransform)transform).sizeDelta = new Vector2(contactInfo.width, contactInfo.height);
    }

    
    private void ButtonListener()
    {
        Debug.Log("Index : " + CellIndex +  ", Name : " + _contactInfo.Name  + ", Gender : " + _contactInfo.Gender);
    }
}
