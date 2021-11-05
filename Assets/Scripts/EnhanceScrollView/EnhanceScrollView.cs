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

    [SerializeField]
    private bool enableDrag = true;

    // Lerp duration
    public float lerpDuration = 0.2f;
    private float mCurrentDuration = 0.0f;
    private int mCenterIndex = 0;
    public bool enableLerpTween = true;

    // center and preCentered item
    EnhanceItem mCurCenterItem;
    private EnhanceItem curCenterItem { 
        get 
        {
            return mCurCenterItem;
        } 
        set 
        {
            mCurCenterItem = value;
            centerCallback?.Invoke(value);
        } 
    }
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
    [SerializeField]
    //[Range(0, int.MaxValue)]
    [Range(0, 2147483583)]
    protected int _maxUniqueIndex = -1;
    public int maxUniqueIndex 
    { 
        get { return _maxUniqueIndex; } 
        set 
        {
            if (value < 0)
                Debug.LogError("maxAmount必须大于等于零, 修正为零");

            _maxUniqueIndex =  Mathf.Max(value, 0);
        } 
    }

    // 刷新Item回调
    public Action<int, Transform> refreshItemCallback { get; private set; }

    // item居中回调
    private Action<EnhanceItem> centerCallback;

    // 是否需要刷新Item，主要用于直接跳转到指定索引时，避免重复刷新
    private bool needRefreshItem = true;

    // 循环模式
    public bool loopMode = false;

    // 回弹值
    public float elastic = 0.03f;
    private float curElastic = 0f;

    // 拖拽因子
    public float dragFactor = 0.001f;
    private float dragValue = 0.0f;

    // "depth" factor (2d widget depth or 3d Z value)
    private int depthFactor = 5;

    // Drag enhance scroll view
    [Tooltip("Camera for drag ray cast")]
    public Camera sourceCamera;

    public void EnableDrag(bool isEnabled)
    {
        enableDrag = isEnabled;
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
            curEnhanceItems.SetEnhanceScrollView(this);
            curEnhanceItems.SetUniqueIndex(i);
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
        elastic = Mathf.Clamp(elastic, 0, dFactor * 0.3F);
        LerpTweenToTarget(0f, curVerticalValue, false);
    
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

    public void Init(int maxAmount, Action<int, Transform> refreshItemCallback, Action<EnhanceItem> centerCallback)
    {
        maxUniqueIndex = maxAmount;
        this.refreshItemCallback = refreshItemCallback;
        this.centerCallback = centerCallback;
        this.ResetUniqueIndex();
        this.OnTweenOver();
    }

    
    // 根据当前值更新Item位置，并更新唯一索引
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
        UpdateShowState();
    }

    void ResetUniqueIndex()
    {
        foreach(var item in listEnhanceItems)
        {
            item.SetUniqueIndex(-1, true);
        }
    }

    void UpdateUniqueIndex()
    {
        if (maxUniqueIndex < 0)
        {
            Debug.LogError("请先调用初始化函数Init");
            return;
        }

        SortEnhanceItem();
        
        foreach (var item in listSortedItems)
        {   
            var itemNewUniqueIndex = curCenterItem.UniqueIndex + (item.RealIndex - curCenterItem.RealIndex);
            item.SetUniqueIndex(itemNewUniqueIndex, !needRefreshItem);
        }
    }

    void UpdateShowState()
    {
        foreach (var item in listSortedItems)
            item.UpdateShowState();
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
        UpdateUniqueIndex();
        UpdateShowState();
        if (preCenterItem != null)
            preCenterItem.SetSelectState(false);
        if (curCenterItem != null)
            curCenterItem.SetSelectState(true);

        needRefreshItem = true;
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
        float centerYValue = GetCenterPos();
        bool isBottom = false;
        if (selectItem.transform.localPosition.y < centerYValue)
            isBottom = true;

        // calculate the offset * dFactor
        int moveIndexCount = GetMoveCurveFactorCount(preCenterItem, selectItem);
        float dvalue = 0.0f;
        if (isBottom)
        {
            dvalue = dFactor * moveIndexCount;
        }
        else
        {
            dvalue = -1 * dFactor * moveIndexCount;
        }
        float originValue = curVerticalValue;
        LerpTweenToTarget(originValue, curVerticalValue + dvalue, true);
    }

    public void JumpToUniqueIndex(int index)
    {
        if (index < 0 || index > maxUniqueIndex) 
        {
            Debug.LogError("索引超出范围");
            return;
        }

        bool isForward = index < curCenterItem.UniqueIndex;
        bool isBackwards = index > curCenterItem.UniqueIndex;
        EnhanceItem jumpToItem = null;
        if (isForward)
        {
            var offset = Mathf.Min(curCenterItem.UniqueIndex - index, mCenterIndex);
            jumpToItem = GetPrevItem(offset);
            
        }
        else if (isBackwards)
        {
            var offset = Mathf.Min(index - curCenterItem.UniqueIndex, mCenterIndex);
            jumpToItem = GetNextItem(offset);
        }

        if(jumpToItem != null)
        {
            //重置唯一索引
            jumpToItem.SetUniqueIndex(index);
            for (int i = 1; i <= mCenterIndex; i++)
            {
                var curPrevItem = GetPrevItem(i, jumpToItem);
                curPrevItem.SetUniqueIndex(index - i);
                curPrevItem.UpdateShowState();
                var curNextItem = GetNextItem(i, jumpToItem);
                curNextItem.SetUniqueIndex(index + i);
            }

            if (listEnhanceItems.Count % 2 == 0)
            {
                int lastIndex = mCenterIndex + 1;
                var curNextItem = GetNextItem(lastIndex, jumpToItem);
                curNextItem.SetUniqueIndex(index + lastIndex);
            }

            needRefreshItem = false;
            SetVerticalTargetItemIndex(jumpToItem);
        }
    }

    public void OnBtnPrevClick()
    {
        if (!canChangeItem || !CanMoveBottom())
            return;

        SetVerticalTargetItemIndex(GetPrevItem());
    }

    
    public void OnBtnNextClick()
    {
        if (!canChangeItem || !CanMoveTop())
            return;
        
        SetVerticalTargetItemIndex(GetNextItem());
    }

    bool CanMoveTop()
    {
        if (loopMode)
            return true;
        
        return curCenterItem.UniqueIndex < maxUniqueIndex - 1;
    }

    bool CanMoveBottom()
    {
        if (loopMode)
            return true;

        return curCenterItem.UniqueIndex > 0;
    }

    /// <summary>
    /// 获取基于指定项的前几条项
    /// </summary>
    /// <param name="amount">数量， 默认1</param>
    /// <param name="baseItem">初始项，默认是中心项</param>
    /// <returns>返回获取的项</returns>
    EnhanceItem GetPrevItem(int amount = 1, EnhanceItem baseItem = null)
    {
        if (baseItem == null)
            baseItem = curCenterItem;

        var targetIndex = baseItem.CurveOffSetIndex + amount;
        var itemAmount = listEnhanceItems.Count;
        if (targetIndex >= itemAmount)
            targetIndex = targetIndex % itemAmount;

        return listEnhanceItems[targetIndex];
    }

    /// <summary>
    /// 获取基于指定项的后几条项
    /// </summary>
    /// <param name="amount">数量， 默认1</param>
    /// <param name="baseItem">初始项，默认是中心项</param>
    /// <returns>返回获取的项</returns>
    EnhanceItem GetNextItem(int amount = 1, EnhanceItem baseItem = null)
    {
        if (baseItem == null)
            baseItem = curCenterItem;

        var targetIndex = baseItem.CurveOffSetIndex - amount;
        if (targetIndex < 0)
            targetIndex = listEnhanceItems.Count - amount;

        return listEnhanceItems[targetIndex];
    }

    // 开始拖拽
    public void OnDragEnhanceViewBegin()
    {
        if (!enableDrag || !canChangeItem)
            return;
    }
 
    // 拖拽中
    public void OnDragEnhanceViewMove(Vector2 delta)
    {
        // 未启用拖拽
        if (!enableDrag || !canChangeItem)
            return;

        // 没有拖拽位移
        if (Mathf.Approximately(delta.y, 0.0F))
            return;

        // 处理边界回弹
        var yDelta = delta.y * dragFactor;
        bool up = yDelta > 0.0f;
        if (up)
        {
            if (!CanMoveTop())
            {
                if (!CheckElastic())
                    return;

                curElastic += yDelta;
            }
            else 
            {
                if (curElastic < 0 )
                    curElastic = Mathf.Min(curElastic + yDelta, 0);
            }
        }
        else
        {
            if (!CanMoveBottom())
            {
                if (!CheckElastic())
                    return;

                curElastic += yDelta;
            }
            else
            {
                if (curElastic > 0)
                    curElastic = Mathf.Max(curElastic + yDelta, 0);
            }
        }
        
        // 更新居中item
        //dragValue += yDelta;
        //if (Mathf.Abs(dragValue) >= dFactor)
        //{
        //    dragValue = 0.0f;
        //    preCenterItem = curCenterItem;
        //    if (yDelta > 0)
        //        curCenterItem = GetPrevItem();
        //    else
        //        curCenterItem = GetNextItem();
        //}

        // 更新列表位置
        curVerticalValue += yDelta;
        LerpTweenToTarget(0.0f, curVerticalValue, false);
    }

    private bool CheckElastic()
    {
        return Mathf.Abs(curElastic) < elastic;
    }

    // 结束拖拽
    public void OnDragEnhanceViewEnd()
    {
        if (!enableDrag || !canChangeItem)
            return;

        canChangeItem = false;
        curElastic = 0.0f;
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
        //Debug.LogFormat("居中项目索引：{0}, originVerticalValue:{1}, target:{2}", closestIndex, originVerticalValue, target);
        LerpTweenToTarget(originVerticalValue, target, true);
    }
}