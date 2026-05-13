using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//此脚本用来实现游戏开始界面飞机模型的旋转
public class Rotate : MonoBehaviour
{   //创建旋转速度
public Vector3  rotateSpeed;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.Rotate(rotateSpeed*Time.deltaTime,Space.Self);
    }
}
