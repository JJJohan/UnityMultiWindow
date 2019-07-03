using UnityEngine;
using UnityEngine.UI;
using JetBrains.Annotations;

public class ButtonController : MonoBehaviour
{
    private Button _redButton;
    private Button _blueButton;
    private bool _coloured;
    private Material _cubeMaterial;

    [UsedImplicitly]
    private void Awake()
    {
        GameObject cubeObject = GameObject.Find("Cube");
        MeshRenderer meshRenderer = cubeObject.GetComponent<MeshRenderer>();
        _cubeMaterial = meshRenderer.sharedMaterial;

        _redButton = GameObject.Find("RedButton").GetComponent<Button>();
        _redButton.onClick.AddListener(OnRedClick);

        _blueButton = GameObject.Find("BlueButton").GetComponent<Button>();
        _blueButton.onClick.AddListener(OnBlueClick);
    }

    private void OnRedClick()
    {
        SetColour(Color.red);
    }

    private void OnBlueClick()
    {
        SetColour(Color.blue);
    }

    private void SetColour(Color colour)
    {
        _coloured = !_coloured;
        if (!_coloured)
        {
            _cubeMaterial.color = Color.white;
        }
        else
        {
            _cubeMaterial.color = colour;
        }
    }
}
