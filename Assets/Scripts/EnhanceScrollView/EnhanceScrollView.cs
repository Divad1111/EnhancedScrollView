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
    // Offset height between item
    public float cellHeight = 10f;
    private float totalVerticalHeight = 500.0f;
    // horizontal fixed position value 
    public float xFixedPositionValue = 46.0f;
    [Range(-1.0f, 1.0f)]
    public float xOffset = 0;

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

    // originVerticalValue Lerp to horizontalTargetValue
    //private float initMinVerticalValue = 0.5f;
    //private float initMaxVerticalValue = 0.1f;
    //private float minVerticalValue = 0.5f;
    //private float maxVerticalValue = 0.1f;
    private float originVerticalValue = 0.1f;
    public float curVerticalValue = 0.5f;

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
        totalVerticalHeight = cellHeight * count;
        curCenterItem = listEnhanceItems[startCenterIndex];
        curVerticalValue = 0.5f - curCenterItem.CenterOffSet;
        //minVerticalValue = curVerticalValue;
        //maxVerticalValue = 10.0f;//dFactor * (count - startCenterIndex);
        //initMinVerticalValue = minVerticalValue + (count - mCenterIndex) * dFactor;
        //initMaxVerticalValue = maxVerticalValue;
        LerpTweenToTarget(0f, curVerticalValue, false);

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
            originVerticalValue = targetValue;
            UpdateEnhanceScrollView(targetValue);
            this.OnTweenOver();
        }
        else
        {
            originVerticalValue = originValue;
            curVerticalValue = targetValue;
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
            float yValue = GetYPosValue(fValue, itemScript.CenterOffSet);
            float scaleValue = GetScaleValue(fValue, itemScript.CenterOffSet);
            float depthCurveValue = depthCurve.Evaluate(fValue + itemScript.CenterOffSet);
            float xOffsetValue = itemScript.GetWidth() * (1 - scaleValue) * 0.5f * xOffset;
            itemScript.UpdateScrollViewItems(yValue, depthCurveValue, depthFactor, listEnhanceItems.Count, xFixedPositionValue + xOffsetValue, scaleValue);
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
        float value = Mathf.Lerp(originVerticalValue, curVerticalValue, percent);
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
    private float GetYPosValue(float sliderValue, float added)
    {
        float evaluateValue = positionCurve.Evaluate(sliderValue + added) * totalVerticalHeight;
        return evaluateValue;
    }
    public float GetCenterPos()
    {
        return positionCurve.Evaluate(0.5f) * totalVerticalHeight;
    }

    private int GetMoveCurveFactorCount(EnhanceItem preCenterItem, EnhanceItem newCenterItem)
    {
        SortEnhanceItem();
        int factorCount = Mathf.Abs(newCenterItem.RealIndex) - Mathf.Abs(preCenterItem.RealIndex);
        return Mathf.Abs(factorCount);
    }

    // sort item with X so we can know how much distance we need to move the timeLine(curve time line)
    static public int SortPosition(EnhanceItem a, EnhanceItem b) { return -1 * a.transform.localPosition.y.CompareTo(b.transform.localPosition.y); }
    private void SortEnhanceItem()
    {
        listSortedItems.Sort(SortPosition);
        for (int i = listSortedItems.Count - 1; i >= 0; i--)
            //listSortedItems[i].RealIndex = i;
            listSortedItems[i].SetRealIndex(i);
    }

    public void SetVerticalTargetItemIndex(EnhanceItem selectItem)
    {
        if (!canChangeItem)
            return;

        if (curCenterItem == selectItem)
            return;

        canChangeItem = false;
        preCenterItem = curCenterItem;
        curCenterItem = selectItem;

        // calculate the direction of moving
        //float centerXValue = positionCurve.Evaluate(0.5f) * totalVerticalHeight;
        float centerXValue = GetCenterPos();
        bool isBottom = false;
        if (selectItem.transform.localPosition.y < centerXValue)
            isBottom = true;

        // calculate the offset * dFactor
        int moveIndexCount = GetMoveCurveFactorCount(preCenterItem, selectItem);
        float dvalue = 0.0f;
        if (isBottom)
        {
            dvalue = -dFactor * moveIndexCount;
        }
        else
        {
            dvalue = dFactor * moveIndexCount;
        }
        float originValue = curVerticalValue;
        LerpTweenToTarget(originValue, curVerticalValue + dvalue, true);
    }

    // Click the right button to select the next item.
    public void OnBtnBottomClick()
    {
        if (!canChangeItem || !CanMoveBottom())
            return;
        //int targetIndex = curCenterItem.CurveOffSetIndex + 1;
        //if (targetIndex > listEnhanceItems.Count - 1)
        //    targetIndex = 0;
        //SetVerticalTargetItemIndex(listEnhanceItems[targetIndex]);


        SetVerticalTargetItemIndex(GetNextItem());
    }

    // Click the left button the select next next item.
    public void OnBtnTopClick()
    {
        if (!canChangeItem || !CanMoveTop())
            return;
        //int targetIndex = curCenterItem.CurveOffSetIndex - 1;
        //if (targetIndex < 0)
        //    targetIndex = listEnhanceItems.Count - 1;
        //SetVerticalTargetItemIndex(listEnhanceItems[targetIndex]);
        SetVerticalTargetItemIndex(GetPrevItem());
    }

    bool CanMoveTop()
    {
        if (loopMode)
            return true;

        return curCenterItem.UniqueIndex > 0;
    }

    bool CanMoveBottom()
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
        if (Mathf.Abs(delta.y) > 0.0f)
        {   
            var yDelta = delta.y * factor;
            if (yDelta > 0.0f)
            {
                //if (curVerticalValue + yDelta > minVerticalValue + dFactor * 0.5F)
                //    return;
                if (!CanMoveTop())
                    return;
            }
            else
            {
                if (!CanMoveBottom())
                    return;
            }

            

            curVerticalValue += yDelta;
            dragValue += yDelta;
            if (Mathf.Abs(dragValue) >= dFactor)
            {
                dragValue = 0.0f;
                preCenterItem = curCenterItem;
                if (yDelta > 0)
                    curCenterItem = GetPrevItem();
                else
                    curCenterItem = GetNextItem();
            }

            LerpTweenToTarget(0.0f, curVerticalValue, false);
        }
    }

    // On Drag End
    public void OnDragEnhanceViewEnd()
    {
        // reset drag Value
        dragValue = 0.0f;
        
        // find closed item to be centered
        int closestIndex = 0;
        float value = (curVerticalValue - (int)curVerticalValue);
        float min = float.MaxValue;
        float tmp = 0.5f * (curVerticalValue < 0 ? -1 : 1);
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
        originVerticalValue = curVerticalValue;
        float target = ((int)curVerticalValue + (tmp - listEnhanceItems[closestIndex].CenterOffSet));
        preCenterItem = curCenterItem;
        curCenterItem = listEnhanceItems[closestIndex];
        LerpTweenToTarget(originVerticalValue, target, true);
        canChangeItem = false;
    }
}