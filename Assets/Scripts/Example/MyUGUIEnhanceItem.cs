﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MyUGUIEnhanceItem : EnhanceItem
{
    public Text txtIndex;
    public Text txtRealIndex;
    public Text dataIndex;
    private Button uButton;
    private RawImage rawImage;

    protected override void OnStart()
    {
        rawImage = GetComponent<RawImage>();
        uButton = GetComponent<Button>();
        uButton.onClick.AddListener(OnClickUGUIButton);
    }

    private void OnClickUGUIButton()
    {
        OnClickEnhanceItem();
    }

    // Set the item "depth" 2d or 3d
    protected override void SetItemDepth(float depthCurveValue, int depthFactor, float itemCount)
    {
        int newDepth = (int)(depthCurveValue * itemCount);
        this.transform.SetSiblingIndex(newDepth);
    }

    public override void SetSelectState(bool isCenter)
    {
        if (rawImage == null)
            rawImage = GetComponent<RawImage>();
        rawImage.color = isCenter ? Color.white : Color.gray;
    }

    public override void SetUniqueIndex(int index, bool onlyUpdateValue = false)
    {
        base.SetUniqueIndex(index, onlyUpdateValue);

        txtIndex.text = index.ToString();
        
        dataIndex.text = DataIndex.ToString();
    }

    public override void SetRealIndex(int index)
    {
        base.SetRealIndex(index);
        txtRealIndex.text = index.ToString();
    }
}
