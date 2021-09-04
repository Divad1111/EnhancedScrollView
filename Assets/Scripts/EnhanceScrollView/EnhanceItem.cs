using UnityEngine;
using System.Collections;

public class EnhanceItem : MonoBehaviour
{
    private int uniqueIndex = 0;
    public virtual int UniqueIndex
    {
        get { return this.uniqueIndex; }
    }
    // Start index
    private int curveOffSetIndex = 0;
    public int CurveOffSetIndex
    {
        get { return this.curveOffSetIndex; }
        set { this.curveOffSetIndex = value; }
    }

    // Runtime real index(Be calculated in runtime)
    private int curRealIndex = 0;
    public int RealIndex
    {
        get { return this.curRealIndex; }
        //set { this.curRealIndex = value; }
    }

    public int DataIndex
    {
        get
        {   
            var maxUniqueIndex = mEsv.maxUniqueIndex;
            int dataIndex = uniqueIndex % maxUniqueIndex;
            if (uniqueIndex < 0)
                dataIndex = Mathf.Abs((maxUniqueIndex + uniqueIndex) % maxUniqueIndex);

            return dataIndex;
        }
    }

    // Curve center offset 
    private float dCurveCenterOffset = 0.0f;
    public float CenterOffSet
    {
        get { return this.dCurveCenterOffset; }
        set { dCurveCenterOffset = value; }
    }
    private Transform mTrs;

    protected EnhanceScrollView mEsv;

    void Awake()
    {
        mTrs = this.transform;
        OnAwake();
    }

    void Start()
    {
        OnStart();
    }

    public void SetEnhanceScrollView(EnhanceScrollView esv)
    {
        mEsv = esv;
    }

    public float GetWidth()
    {
        return (mTrs as RectTransform).sizeDelta.x;
    }

    public float GetHeight()
    {
        return (mTrs as RectTransform).sizeDelta.y;
    }

    // Update Item's status
    // 1. position
    // 2. scale
    // 3. "depth" is 2D or z Position in 3D to set the front and back item
    public void UpdateScrollViewItems(
        float yValue,
        float depthCurveValue,
        int depthFactor,
        float itemCount,
        float xValue,
        float scaleValue
        )
    {
        Vector3 targetPos = Vector3.zero;
        Vector3 targetScale = Vector3.one;
        // position
        targetPos.x = xValue;
        targetPos.y = yValue;
        mTrs.localPosition = targetPos;

        // Set the "depth" of item
        // targetPos.z = depthValue;
        SetItemDepth(depthCurveValue, depthFactor, itemCount);
        // scale
        targetScale.x = targetScale.y = scaleValue;
        mTrs.localScale = targetScale;
    }

    public void Show(bool isShow)
    {
        gameObject.SetActive(isShow);
    }

    public bool IsShow()
    {
        return gameObject.activeSelf;
    }

    protected virtual void OnClickEnhanceItem()
    {
        if(mEsv != null)
            mEsv.SetVerticalTargetItemIndex(this);
    }

    protected virtual void OnStart()
    {
    }

    protected virtual void OnAwake()
    {
    }

    protected virtual void SetItemDepth(float depthCurveValue, int depthFactor, float itemCount)
    {
    }

    // Set the item center state
    public virtual void SetSelectState(bool isCenter)
    {
    }

    public virtual void SetRealIndex(int index)
    {
        curRealIndex = index;
    }

    public virtual void SetUniqueIndex(int index)
    {   
        if (uniqueIndex != index || index == 0)
        {
            uniqueIndex = index;
            if (mEsv != null && mEsv.refreshItemCallback != null)
                mEsv.refreshItemCallback.Invoke(DataIndex, transform);
        }
    }
}
