using System;
using UnityEngine;
using System.Collections;
using System.Numerics;
using UnityEngine.UIElements;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

public class Rigid_Bunny : MonoBehaviour 
{
	bool launched 		= false;
	float dt 			= 0.002f;
	Vector3 v 			= new Vector3(0, 0, 0);	// velocity
	Vector3 w 			= new Vector3(0, 0, 0);	// angular velocity
	
	float mass;									// mass
	Matrix4x4 I_ref;							// reference inertia

	float linear_decay	= 0.999f;				// for velocity decay
	float angular_decay	= 0.98f;				
	float restitution 	= 0.5f;					// for collision

	Vector3[] vertices;//all point position
	float m=20;//each point mass

	private float un = 0.5f;//反弹系数
	private float ut = 0.5f;//摩擦系数
	
	// Use this for initialization
	void Start () 
	{		
		Mesh  mesh = GetComponent<MeshFilter>().mesh;
		vertices = mesh.vertices;
		
		
		mass=0;
		for (int i=0; i<vertices.Length; i++) 
		{
			mass += m;
			float diag=m*vertices[i].sqrMagnitude;
			I_ref[0, 0]+=diag;
			I_ref[1, 1]+=diag;
			I_ref[2, 2]+=diag;
			I_ref[0, 0]-=m*vertices[i][0]*vertices[i][0];
			I_ref[0, 1]-=m*vertices[i][0]*vertices[i][1];
			I_ref[0, 2]-=m*vertices[i][0]*vertices[i][2];
			I_ref[1, 0]-=m*vertices[i][1]*vertices[i][0];
			I_ref[1, 1]-=m*vertices[i][1]*vertices[i][1];
			I_ref[1, 2]-=m*vertices[i][1]*vertices[i][2];
			I_ref[2, 0]-=m*vertices[i][2]*vertices[i][0];
			I_ref[2, 1]-=m*vertices[i][2]*vertices[i][1];
			I_ref[2, 2]-=m*vertices[i][2]*vertices[i][2];
		}
		I_ref [3, 3] = 1;
		
	}
	
	Matrix4x4 Get_Cross_Matrix(Vector3 a)
	{
		//Get the cross product matrix of vector a
		Matrix4x4 A = Matrix4x4.zero;
		A [0, 0] = 0; 
		A [0, 1] = -a [2]; 
		A [0, 2] = a [1]; 
		A [1, 0] = a [2]; 
		A [1, 1] = 0; 
		A [1, 2] = -a [0]; 
		A [2, 0] = -a [1]; 
		A [2, 1] = a [0]; 
		A [2, 2] = 0; 
		A [3, 3] = 1;
		return A;
	}

	// In this function, update v and w by the impulse due to the collision with
	//a plane <P, N>
	void Collision_Impulse(Vector3 P, Vector3 N)
	{
		Quaternion q = transform.rotation;
		Vector3 position = transform.position;
		Matrix4x4 R=Matrix4x4.Rotate(q);
		Matrix4x4 I = R * I_ref * R.transpose;
		for (int i = 0; i <vertices.Length; i++)
		{
			Vector3 xi= position + q * vertices[i];
			if (DistanceToPlane(xi ,P,N)<0)
			{
				Vector3 vi = v + Vector3.Cross(w, q*vertices[i]);
				Vector3 J=Vector3.one;
				if (Vector3.Dot(vi,N)<0)
				{
					Vector3 v_Ni = Vector3.Dot(vi, N) * N;
					Vector3 v_Ti = vi - v_Ni;
					float a = Mathf.Max(1-ut*(1+un)*v_Ni.magnitude/v_Ti.magnitude, 0);
					Vector3 v_Ni_new = -un * v_Ni;
					Vector3 v_Ti_new = a * v_Ti;
					Vector3 vi_new = v_Ni_new + v_Ti_new;

					Vector4 Rri4 = R * vertices[i];
					Vector3  Rri3= new Vector3(Rri4.x, Rri4.y, Rri4.z);

					Matrix4x4 Rri4_matrix4x4= (Get_Cross_Matrix(Rri3));
					Matrix4x4 K =twoMatrix4X4AddOrSubtract(NumberXMatrix(1.0f/mass,Matrix4x4.identity),Rri4_matrix4x4 * I.inverse * Rri4_matrix4x4,"-") ;
					J= K.inverse * (vi_new - vi);
					
					v = v + J / mass;
					w=w+(Vector3)(I.inverse*(Get_Cross_Matrix(Rri3)*J));
				}

				
			}
			
		}
	
		
	}

	/// <summary>
	/// 点距离面的函数
	/// </summary>
	/// <param name="x">点位置</param>
	/// <param name="P">面上一个点的位置</param>
	/// <param name="N">面的法向量</param>
	/// <returns></returns>
	float DistanceToPlane(Vector3 x,Vector3 P,Vector3 N)
	{
		return Vector3.Dot((x - P),N) ;
	}
	
	Matrix4x4 NumberXMatrix(float number, Matrix4x4 martix)
	{
		Matrix4x4 result = Matrix4x4.zero;
		for (int i = 0; i < 16; i++)
		{
			result[i] = martix[i] * number;
		}

		return result;
	}

	Matrix4x4 twoMatrix4X4AddOrSubtract(Matrix4x4 m1, Matrix4x4 m2,string symble)
	{
		Matrix4x4 reslut=Matrix4x4.zero;
		if (symble.Equals("+"))
		{
			for (int i = 0; i < 16; i++)
			{
				reslut[i] = m1[i] + m2[i];
			}
		}

		if (symble.Equals("-"))
		{
			for (int i = 0; i < 16; i++)
			{
				reslut[i] = m1[i] - m2[i];
			}
		}
		return reslut;
	}

	Quaternion QuaternionAddOrSubtract(Quaternion q1,Quaternion q2,string symble)
	{
		Quaternion result = Quaternion.identity;
		if (symble.Equals("+"))
		{
			result.x=q1.x + q2.x;
			result.y=q1.y + q2.y;
			result.y=q1.z + q2.z;
			result.w = q1.w + q2.w;
		}

		if (symble.Equals("-"))
		{
			result.x=q1.x - q2.x;
			result.y=q1.y - q2.y;
			result.y=q1.z - q2.z;
			result.w = q1.w - q2.w;
		}
		return result;
	}

	// Update is called once per frame
	void Update () 
	{
		//Game Control
		if(Input.GetKey("r"))
		{
			transform.position = new Vector3 (0, 0.6f, 0);
			restitution = 0.5f;
			launched=false;
		}
		if(Input.GetKey("l"))
		{
			v = new Vector3 (10, 0, 0);
			w = new Vector3 (20, 20, 20);
			launched=true;
		}

		if (launched)
		{
			// Part I: Update velocities
			Vector3 FTotal = mass * Physics.gravity*5;//只受到重力
			v = v + (FTotal / mass) * dt;

			// Part II: Collision Impulse
			Collision_Impulse(new Vector3(0, 0.01f, 0), new Vector3(0, 1, 0));
			Collision_Impulse(new Vector3(2, 0, 0), new Vector3(-1, 0, 0));

			// Part III: Update position & orientation
			//Update linear status
			Vector3 x = transform.position;
			x = x + v * dt;

			//Update angular status
			Quaternion q = transform.rotation;
			Vector3 torqueTotal = Vector3.zero;
			Matrix4x4 R = Matrix4x4.Rotate(q);
			for (int i = 0; i < vertices.Length; i++)
			{

				Vector3 Rri = q * vertices[i];
				Vector3 torquei = Vector3.Cross(Rri, m * Physics.gravity);
				torqueTotal += torquei;
			}

			//current inertia
			Matrix4x4 I = R * I_ref * R.transpose;
			w += (Vector3) (I.inverse * torqueTotal * dt);

			Quaternion q1 = new Quaternion(w.x * dt / 2, w.y * dt / 2, w.z * dt / 2, 0);
			q = QuaternionAddOrSubtract(q,q1*q,"+") ;
			// Part IV: Assign to the object
			transform.position = x;
			transform.rotation = q;
		}
	}
	

}




