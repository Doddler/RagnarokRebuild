﻿using System.Collections;
using System.Collections.Generic;
using System.Text;
using Assets.Scripts;
using Assets.Scripts.Utility;
using RebuildData.Shared.Enum;
using TMPro;
using UnityEngine;

public class DamageIndicator : MonoBehaviour
{
	public TextMeshPro TextObject;
	public AnimationCurve Trajectory;
	public AnimationCurve Size;
	public AnimationCurve Alpha;

	private static StringBuilder sb = new StringBuilder(128);

	private Vector3 start;
	private Vector3 end;

	public void DoDamage(int value, Vector3 startPosition, float height, Direction direction, bool isRed, bool isCrit)
	{
		var text = value.ToString();

		if (isRed)
			sb.Append("<color=#FF0000>");

		var useTrueType = CameraFollower.Instance.UseTTFDamage;

		if (useTrueType)
			sb.Append("<cspace=0.4>");

		foreach (var c in text)
		{
			if(!useTrueType)
				sb.Append("<sprite=");
			sb.Append(c);
			if (!useTrueType)
			{
				if (isRed)
					sb.Append(" tint");
				sb.Append(">");
			}
		}

		TextObject.text = sb.ToString();
		sb.Clear();

		var vec = -direction.GetVectorValue();
		var dirVector = new Vector3(vec.x, 0, vec.y);

		start = new Vector3(startPosition.x, startPosition.y + height * 1.25f, startPosition.z);
		//end = start;
		end = start + dirVector * 4;

		transform.localPosition = start;

		var lt = LeanTween.value(gameObject, OnUpdate, 0, 1, 1f);
		lt.setOnComplete(onComplete: () => GameObject.Destroy(gameObject));
	}

	void OnUpdate(float f)
	{
		var height = Trajectory.Evaluate(f);
		var size = Size.Evaluate(f);
		var pos = Vector3.Lerp(start, end, f);
		var alpha = Alpha.Evaluate(f);

		transform.localPosition = new Vector3(pos.x, pos.y + height * 6, pos.z);
		transform.localScale = new Vector3(size, size, size);
		TextObject.color = new Color(1, 1, 1, alpha);
	}

}
