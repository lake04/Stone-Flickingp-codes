using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterDetailScroll : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const int SIZE = 2;
    private float[] pos = new float[SIZE];
    private float distance;
    public int TargetIndex { get; private set; }
    public float TargetPos { get; private set; }

    [SerializeField] PageIndicator pageIndicator;

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        distance = 1f / (SIZE - 1);
        for (int i = 0; i < SIZE; i++)
        {
            pos[i] = distance * i;
        }
    }

    public void UpdateTargetByScrollValue(float scrollValue, float deltaX)
    {
        int newIndex = TargetIndex;
        float newPos = TargetPos;

        for (int i = 0; i < SIZE; i++)
        {
            if (scrollValue < pos[i] + distance * 0.5f && scrollValue > pos[i] - distance * 0.5f)
            {
                newIndex = i;
                newPos = pos[i];
                break;
            }
        }

        if (Mathf.Abs(deltaX) >1f)
        {
            if (deltaX > 0 && newIndex > 0) newIndex--;
            else if (deltaX < 0 && newIndex < SIZE - 1) newIndex++;
            newPos = pos[newIndex];
        }

        TargetIndex = newIndex;
        TargetPos = newPos;
        Debug.Log($"TargetIndex: {TargetIndex}, TargetPos: {TargetPos}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {

    }

    public void OnDrag(PointerEventData eventData)
    {

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"OnEndDrag: {eventData.delta}");
        UpdateTargetByScrollValue(pageIndicator.indicator.transform.position.x, eventData.delta.x);
    }
}
