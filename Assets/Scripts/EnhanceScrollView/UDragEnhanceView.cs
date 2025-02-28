﻿using UnityEngine;
using System.Collections;
using System.Diagnostics;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Debug = UnityEngine.Debug;

public class UDragEnhanceView : EventTrigger
{
    private EnhanceScrollView enhanceScrollView;
    public void SetScrollView(EnhanceScrollView view)
    {
        enhanceScrollView = view;
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        base.OnBeginDrag(eventData);
        if (enhanceScrollView != null)
            enhanceScrollView.OnDragEnhanceViewBegin();
    }

    public override void OnDrag(PointerEventData eventData)
    {
        base.OnDrag(eventData);
        if (enhanceScrollView != null)
            enhanceScrollView.OnDragEnhanceViewMove(eventData.delta);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        base.OnEndDrag(eventData);
        if (enhanceScrollView != null)
            enhanceScrollView.OnDragEnhanceViewEnd();
    }
}
