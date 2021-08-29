using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(UDragEnhanceView))]
public class EnhanceScrollView : MonoBehaviour
{
    // Control the item's scale curve
    public AnimationCurve scaleCurve;
    // Control the position curve
    public AnimationCurve positionCurve;
    // Control the depth
    public AnimationCurve depthCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
    // The start center index
    [Tooltip("The Start center index")]
    public int startCenterIndex = 0;
    // Offset width between item
    public float cellWidth = 10f;
    private float totalHorizontalWidth = 500.0f;
    // vertical fixed position value 
    public float yFixedPositionValue = 46.0f;

    // Lerp duration
    public float lerpDuration = 0.2f;
    private float mCurrentDuration = 0.0f;
    private int mCenterIndex = 0;
    public bool enableLerpTween = true;

    // center and preCentered item
    private EnhanceItem curCenterItem;
    private EnhanceItem preCenterItem;

    // if we can change the target item
    private bool canChangeItem = true;
    private float dFactor = 0.2f;

    // originHorizontalValue Lerp to horizontalTargetValue
    //private float initMinHorizontalValue = 0.5f;
    //private float initMaxHorizontalValue = 0.1f;
    private float minHorizontalValue = 0.5f;
    private float maxHorizontalValue = 0.1f;
    private float originHorizontalValue = 0.1f;
    public float curHorizontalValue = 0.5f;

    // 最大唯一索引
    private int maxUniqueIndex = 50;
    private Action<int, Transform> refreshItemCallback;

    // 循环模式
    public bool loopMode = false;

    // "depth" factor (2d widget depth or 3d Z value)
    private int depthFactor = 5;

    // Drag enhance scroll view
    [Tooltip("Camera for drag ray cast")]
    public Camera sourceCamera;

    public void EnableDrag(bool isEnabled)
    {
        
    }

    // targets enhance item in scroll view
    public List<EnhanceItem> listEnhanceItems;
    // sort to get right index
    private List<EnhanceItem> listSortedItems = new List<EnhanceItem>();

    private static EnhanceScrollView instance;
    public static EnhanceScrollView GetInstance
    {
        get { return instance; }
    }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        canChangeItem = true;
        int count = listEnhanceItems.Count;
        dFactor = (Mathf.RoundToInt((1f / count) * 10000f)) * 0.0001f;
        mCenterIndex = count / 2;
        if (count % 2 == 0)
            mCenterIndex = count / 2 - 1;
        int index = 0;
        for (int i = count - 1; i >= 0; i--)
        {
            var curEnhanceItems = listEnhanceItems[i];
            curEnhanceItems.CurveOffSetIndex = i;
            curEnhanceItems.CenterOffSet = dFactor * (mCenterIndex - index);
            curEnhanceItems.SetUniqueIndex(i);
            curEnhanceItems.SetEnhanceScrollView(this);
            curEnhanceItems.SetSelectState(false);
            GameObject obj = curEnhanceItems.gameObject;

            UDragEnhanceView script = obj.GetComponent<UDragEnhanceView>();
            if (script != null)
                script.SetScrollView(this);
            
            index++;
        }

        GetComponent<UDragEnhanceView>().SetScrollView(this);

        // set the center item with startCenterIndex
        if (startCenterIndex < 0 || startCenterIndex >= count)
        {
            Debug.LogError("## startCenterIndex < 0 || startCenterIndex >= listEnhanceItems.Count  out of index ##");
            startCenterIndex = mCenterIndex;
        }

        // sorted items
        listSortedItems = new List<EnhanceItem>(listEnhanceItems.ToArray());
        totalHorizontalWidth = cellWidth * count;
        curCenterItem = listEnhanceItems[startCenterIndex];
        curHorizontalValue = 0.5f - curCenterItem.CenterOffSet;
        minHorizontalValue = curHorizontalValue;
        //maxHorizontalValue = 10.0f;//dFactor * (count - startCenterIndex);
        //initMinHorizontalValue = minHorizontalValue + (count - mCenterIndex) * dFactor;
        //initMaxHorizontalValue = maxHorizontalValue;
        LerpTweenToTarget(0f, curHorizontalValue, false);

        // 
        // enable the drag actions
        // 
        EnableDrag(true);
    }

    private void LerpTweenToTarget(float originValue, float targetValue, bool needTween = false)
    {   
        if (!needTween)
        {
            SortEnhanceItem();
            originHorizontalValue = targetValue;
            UpdateEnhanceScrollView(targetValue);
            this.OnTweenOver();
        }
        else
        {
            originHorizontalValue = originValue;
            curHorizontalValue = targetValue;
            mCurrentDuration = 0.0f;
        }
        enableLerpTween = needTween;
    }

    public void DisableLerpTween()
    {
        this.enableLerpTween = false;
    }

    public void Init(int maxAmount, Action<int, Transform> refreshItemCallback)
    {
        maxUniqueIndex = maxAmount;
        this.refreshItemCallback = refreshItemCallback;
    }

    /// 
    /// Update EnhanceItem state with curve fTime value
    /// 
    public void UpdateEnhanceScrollView(float fValue)
    {
        for (int i = 0; i < listEnhanceItems.Count; i++)
        {
            EnhanceItem itemScript = listEnhanceItems[i];
            float xValue = GetXPosValue(fValue, itemScript.CenterOffSet);
            float scaleValue = GetScaleValue(fValue, itemScript.CenterOffSet);
            float depthCurveValue = depthCurve.Evaluate(fValue + itemScript.CenterOffSet);
            itemScript.UpdateScrollViewItems(xValue, depthCurveValue, depthFactor, listEnhanceItems.Count, yFixedPositionValue, scaleValue);
        }

        UpdateUniqueIndex();
    }

    void UpdateUniqueIndex()
    {
        if (maxUniqueIndex <= 0)
        {
            Debug.LogError("请先调用初始化函数Init");
            return;
        }

        SortEnhanceItem();
        
        foreach (var item in listSortedItems)
        {
            var itemUniqueIndex = item.UniqueIndex;
            var itemNewUniqueIndex = curCenterItem.UniqueIndex + (item.RealIndex - curCenterItem.RealIndex);
            bool needRefresh = itemUniqueIndex != itemNewUniqueIndex;

            if (loopMode)
            {
                item.Show(true);
                if (needRefresh)
                {
                    if (itemNewUniqueIndex < 0)
                        refreshItemCallback?.Invoke((maxUniqueIndex + itemNewUniqueIndex) % maxUniqueIndex, item.transform);
                    else
                        refreshItemCallback?.Invoke(itemNewUniqueIndex % maxUniqueIndex, item.transform);
                }   
            }
            else
            {
                bool needShow = itemNewUniqueIndex >= 0 && itemNewUniqueIndex < maxUniqueIndex;
                item.Show(needShow);
                if (needShow && needRefresh)
                    refreshItemCallback?.Invoke(itemNewUniqueIndex % maxUniqueIndex, item.transform);
            }
            
            item.SetUniqueIndex(itemNewUniqueIndex);
        }
    }

    void Update()
    {
        if (enableLerpTween)
            TweenViewToTarget();
    }

    private void TweenViewToTarget()
    {
        mCurrentDuration += Time.deltaTime;
        if (mCurrentDuration > lerpDuration)
            mCurrentDuration = lerpDuration;

        float percent = mCurrentDuration / lerpDuration;
        float value = Mathf.Lerp(originHorizontalValue, curHorizontalValue, percent);
        UpdateEnhanceScrollView(value);
        if (mCurrentDuration >= lerpDuration)
        {
            canChangeItem = true;
            enableLerpTween = false;
            OnTweenOver();
        }
    }

    private void OnTweenOver()
    {
        if (preCenterItem != null)
            preCenterItem.SetSelectState(false);
        if (curCenterItem != null)
            curCenterItem.SetSelectState(true);
    }

    // Get the evaluate value to set item's scale
    private float GetScaleValue(float sliderValue, float added)
    {
        float scaleValue = scaleCurve.Evaluate(sliderValue + added);
        return scaleValue;
    }

    // Get the X value set the Item's position
    private float GetXPosValue(float sliderValue, float added)
    {
        float evaluateValue = positionCurve.Evaluate(sliderValue + added) * totalHorizontalWidth;
        return evaluateValue;
    }
    public float GetCenterPos()
    {
        return positionCurve.Evaluate(0.5f) * totalHorizontalWidth;
    }

    //private bool IsVisible(float sliderValue, EnhanceItem item)
    //{
    //    if (sliderValue < initMinHorizontalValue)
    //        return IsRight(item);

    //    if (sliderValue > initMaxHorizontalValue)
    //        return IsLeft(item);

    //    return true;
    //}
    

    //private bool IsRight(EnhanceItem item)
    //{
    //    float centerXValue = GetCenterPos();
        
    //    if (item.transform.localPosition.x >= centerXValue)
    //        return true;

    //    return false;
    //}

    //private bool IsLeft(EnhanceItem item)
    //{
    //    float centerXValue = GetCenterPos();

    //    if (item.transform.localPosition.x <= centerXValue)
    //        return true;

    //    return false;
    //}


    private int GetMoveCurveFactorCount(EnhanceItem preCenterItem, EnhanceItem newCenterItem)
    {
        SortEnhanceItem();
        int factorCount = Mathf.Abs(newCenterItem.RealIndex) - Mathf.Abs(preCenterItem.RealIndex);
        return Mathf.Abs(factorCount);
    }

    // sort item with X so we can know how much distance we need to move the timeLine(curve time line)
    static public int SortPosition(EnhanceItem a, EnhanceItem b) { return a.transform.localPosition.x.CompareTo(b.transform.localPosition.x); }
    private void SortEnhanceItem()
    {
        listSortedItems.Sort(SortPosition);
        for (int i = listSortedItems.Count - 1; i >= 0; i--)
            //listSortedItems[i].RealIndex = i;
            listSortedItems[i].SetRealIndex(i);
    }

    public void SetHorizontalTargetItemIndex(EnhanceItem selectItem)
    {
        if (!canChangeItem)
            return;

        if (curCenterItem == selectItem)
            return;

        canChangeItem = false;
        preCenterItem = curCenterItem;
        curCenterItem = selectItem;

        // calculate the direction of moving
        //float centerXValue = positionCurve.Evaluate(0.5f) * totalHorizontalWidth;
        float centerXValue = GetCenterPos();
        bool isRight = false;
        if (selectItem.transform.localPosition.x > centerXValue)
            isRight = true;

        // calculate the offset * dFactor
        int moveIndexCount = GetMoveCurveFactorCount(preCenterItem, selectItem);
        float dvalue = 0.0f;
        if (isRight)
        {
            dvalue = -dFactor * moveIndexCount;
        }
        else
        {
            dvalue = dFactor * moveIndexCount;
        }
        float originValue = curHorizontalValue;
        LerpTweenToTarget(originValue, curHorizontalValue + dvalue, true);
    }

    // Click the right button to select the next item.
    public void OnBtnRightClick()
    {
        if (!canChangeItem || !CanMoveRight())
            return;
        //int targetIndex = curCenterItem.CurveOffSetIndex + 1;
        //if (targetIndex > listEnhanceItems.Count - 1)
        //    targetIndex = 0;
        //SetHorizontalTargetItemIndex(listEnhanceItems[targetIndex]);


        SetHorizontalTargetItemIndex(GetNextItem());
    }

    // Click the left button the select next next item.
    public void OnBtnLeftClick()
    {
        if (!canChangeItem || !CanMoveLeft())
            return;
        //int targetIndex = curCenterItem.CurveOffSetIndex - 1;
        //if (targetIndex < 0)
        //    targetIndex = listEnhanceItems.Count - 1;
        //SetHorizontalTargetItemIndex(listEnhanceItems[targetIndex]);
        SetHorizontalTargetItemIndex(GetPrevItem());
    }

    bool CanMoveLeft()
    {
        if (loopMode)
            return true;

        return curCenterItem.UniqueIndex > 0;
    }

    bool CanMoveRight()
    {
        if (loopMode)
            return true;

        return curCenterItem.UniqueIndex < maxUniqueIndex; 
    }

    EnhanceItem GetPrevItem()
    {
        var targetIndex = curCenterItem.CurveOffSetIndex - 1;
        if (targetIndex < 0)
            targetIndex = listEnhanceItems.Count - 1;

        return listEnhanceItems[targetIndex];
    }

    EnhanceItem GetNextItem()
    {
        var targetIndex = curCenterItem.CurveOffSetIndex + 1;
        if (targetIndex > listEnhanceItems.Count - 1)
            targetIndex = 0;

        return listEnhanceItems[targetIndex];
    }

    public float factor = 0.001f;
    private float dragValue = 0.0f;
    // On Drag Move
    public void OnDragEnhanceViewMove(Vector2 delta)
    {   
        if (Mathf.Abs(delta.x) > 0.0f)
        {   
            var xDelta = delta.x * factor;
            if (xDelta > 0.0f)
            {
                //if (curHorizontalValue + xDelta > minHorizontalValue + dFactor * 0.5F)
                //    return;
                if (!CanMoveLeft())
                    return;
            }
            else
            {
                if (!CanMoveRight())
                    return;
            }

            

            curHorizontalValue += xDelta;
            dragValue += xDelta;
            if (Mathf.Abs(dragValue) >= dFactor)
            {
                dragValue = 0.0f;
                preCenterItem = curCenterItem;
                if (xDelta > 0)
                    curCenterItem = GetPrevItem();
                else
                    curCenterItem = GetNextItem();
            }

            LerpTweenToTarget(0.0f, curHorizontalValue, false);
        }
    }

    // On Drag End
    public void OnDragEnhanceViewEnd()
    {
        // reset drag Value
        dragValue = 0.0f;
        
        // find closed item to be centered
        int closestIndex = 0;
        float value = (curHorizontalValue - (int)curHorizontalValue);
        float min = float.MaxValue;
        float tmp = 0.5f * (curHorizontalValue < 0 ? -1 : 1);
        for (int i = 0; i < listEnhanceItems.Count; i++)
        {
            var item = listEnhanceItems[i];
            float dis = Mathf.Abs(Mathf.Abs(value) - Mathf.Abs((tmp - item.CenterOffSet)));
            if (item.IsShow() && dis < min)
            {
                closestIndex = i;
                min = dis;
            }
        }
        originHorizontalValue = curHorizontalValue;
        float target = ((int)curHorizontalValue + (tmp - listEnhanceItems[closestIndex].CenterOffSet));
        preCenterItem = curCenterItem;
        curCenterItem = listEnhanceItems[closestIndex];
        LerpTweenToTarget(originHorizontalValue, target, true);
        canChangeItem = false;
    }
}