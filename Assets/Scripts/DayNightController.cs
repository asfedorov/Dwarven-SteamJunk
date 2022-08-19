using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public struct DayTime
{
    public string name;
    public float duration;
    public Color ambientColorStart;
    public Color ambientColorEnd;
    public float ambientIntensityStart;
    public float ambientIntensityEnd;
    public float pointIntensityStart;
    public float pointIntensityEnd;
    public float shadowStrengthStart;
    public float shadowStrengthEnd;
    public Vector3 pointLightPos;
}



public class DayNightController : MonoBehaviour
{
    public float dayDuration = 13f * 60f;
    public float nightDuration = 7f * 60f;

    float _currentTime = 0f;

    public UnityEngine.Rendering.Universal.Light2D pointLight;
    public UnityEngine.Rendering.Universal.Light2D globalLight;

    public DayTime[] dayTime;

    public int currentDayTime;


    void Start()
    {
        _currentTime = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        _currentTime += Time.deltaTime;
        if (_currentTime >= dayTime[currentDayTime].duration)
        {
            currentDayTime += 1;
            if (currentDayTime >= dayTime.Length)
            {
                currentDayTime = 0;
            }
            _currentTime = 0f;
        }

        globalLight.color = Color.Lerp(
            dayTime[currentDayTime].ambientColorStart,
            dayTime[currentDayTime].ambientColorEnd,
            _currentTime / dayTime[currentDayTime].duration
        );

        globalLight.intensity = Mathf.Lerp(
            dayTime[currentDayTime].ambientIntensityStart,
            dayTime[currentDayTime].ambientIntensityEnd,
            _currentTime / dayTime[currentDayTime].duration
        );

        pointLight.intensity = Mathf.Lerp(
            dayTime[currentDayTime].pointIntensityStart,
            dayTime[currentDayTime].pointIntensityEnd,
            _currentTime / dayTime[currentDayTime].duration
        );

        pointLight.shadowIntensity = Mathf.Lerp(
            dayTime[currentDayTime].shadowStrengthStart,
            dayTime[currentDayTime].shadowStrengthEnd,
            _currentTime / dayTime[currentDayTime].duration
        );

        if (pointLight.transform.position != dayTime[currentDayTime].pointLightPos)
        {
            pointLight.transform.position = dayTime[currentDayTime].pointLightPos;
        }
    }
}
