using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//本脚本借助unity自带物理系统实现前进，减速到停止，旋转
public class MyBox : MonoBehaviour
{   [Tooltip("前进动力")]
      public float zForce=100f;//前进时加速的力
      [Tooltip("减速时的加速度")]
   public  float slowingSpeed=5f;//减速加速度
   [Tooltip("扭矩")]
   public float  torque=1000f;//扭矩
   [Tooltip("倒车加速度")]
   public float  goBackSpeed;//倒车加速度
    Rigidbody rb;  // 
    private float axisWS;//获取纵向轴输入
    private float axisAD;//获取横向轴输入
    // Start is called before the first frame update
    void Start()
    {
        rb =this. GetComponent<Rigidbody>();//获取刚体组件

    }

    // Update is called once per frame
    void Update()
    {  
        //float speed = 1.0f;
        //transform.Translate(speed * Time.deltaTime, 0, 0);
    }

    void FixedUpdate()
    {    
        //实现纵向运动
         axisWS=Input.GetAxis("Vertical");
        if (axisWS>0)
        {
           
            rb.AddRelativeForce(0, 0, zForce );//当按下w时给木块向前的力
        }
        else if(axisWS<0)
        {
           Vector3 speed=this.rb.velocity;//当前的速度向量
           float deltaSpeed=slowingSpeed*Time.fixedDeltaTime;//每0.02秒的速度减小量
           Vector3  direction=speed.normalized;//获取当前速度的方向（是一个三维单位矢量）
           if(speed.magnitude>deltaSpeed)
            {
              rb.AddRelativeForce( direction*(-1)*deltaSpeed,ForceMode.VelocityChange);
            }
            else
            {
                rb.velocity=new Vector3(0,0,0);
            }
        }
        //实现横向转头
        axisAD=Input.GetAxis("Horizontal");
       if(axisAD > 0)
{
    rb.AddRelativeTorque(0, torque, 0);
}
else if(axisAD < 0)  // 改为 else if
{  
    rb.AddRelativeTorque(0, -torque, 0);
}
// axisAD == 0 时不施加扭矩，保持当前旋转状态
        
    }
}
//Input.GetButton("Fire1")检测鼠标左键按下
//rb.AddForce(0, 0, 11 );加牛顿外力