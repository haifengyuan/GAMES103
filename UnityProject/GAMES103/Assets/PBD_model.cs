using UnityEngine;
using System.Collections;
using System.Runtime.Serialization.Json;
using UnityEngine.UIElements;

public class PBD_model: MonoBehaviour {

	float 		t= 0.0333f;
	float		damping= 0.99f;
	int[] 		E;
	float[] 	L;
	Vector3[] 	V;

	private Mesh mesh;
	private Vector3[] X;
	private Vector2[] UV;
	
	private GameObject Sphere_go;
	private Vector3 c;//center of Sphere
	private float r = 2.7f;//radius of Sphere
	// Use this for initialization
	
	Vector3[] sum_x;
	int[] sum_n ;
	void Start () 
	{
		Sphere_go = GameObject.Find("Sphere");
		c = Sphere_go.transform.position;
		mesh = GetComponent<MeshFilter> ().mesh;
		
	

		//Resize the mesh.
		int n=21;
		X = new Vector3[n*n];
		UV= new Vector2[n*n];
		
		
		sum_x=new Vector3[X.Length];
		sum_n = new int[X.Length];
	
		
		int[] T	= new int[(n-1)*(n-1)*6];
		for(int j=0; j<n; j++)
		for(int i=0; i<n; i++)
		{
			X[j*n+i] =new Vector3(5-10.0f*i/(n-1), 0, 5-10.0f*j/(n-1));
			UV[j*n+i]=new Vector3(i/(n-1.0f), j/(n-1.0f));
		}
		int t=0;
		for(int j=0; j<n-1; j++)
		for(int i=0; i<n-1; i++)	
		{
			T[t*6+0]=j*n+i;
			T[t*6+1]=j*n+i+1;
			T[t*6+2]=(j+1)*n+i+1;
			T[t*6+3]=j*n+i;
			T[t*6+4]=(j+1)*n+i+1;
			T[t*6+5]=(j+1)*n+i;
			t++;
		}
		mesh.vertices	= X;
		mesh.triangles	= T;
		mesh.uv 		= UV;
		mesh.RecalculateNormals ();

		//Construct the original edge list
		int[] _E = new int[T.Length*2];
		for (int i=0; i<T.Length; i+=3) 
		{
			_E[i*2+0]=T[i+0];
			_E[i*2+1]=T[i+1];
			_E[i*2+2]=T[i+1];
			_E[i*2+3]=T[i+2];
			_E[i*2+4]=T[i+2];
			_E[i*2+5]=T[i+0];
		}
		//Reorder the original edge list
		for (int i=0; i<_E.Length; i+=2)
			if(_E[i] > _E[i + 1]) 
				Swap(ref _E[i], ref _E[i+1]);
		//Sort the original edge list using quicksort
		Quick_Sort (ref _E, 0, _E.Length/2-1);

		int e_number = 0;
		for (int i=0; i<_E.Length; i+=2)
			if (i == 0 || _E [i + 0] != _E [i - 2] || _E [i + 1] != _E [i - 1]) 
				e_number++;

		E = new int[e_number * 2];
		for (int i=0, e=0; i<_E.Length; i+=2)
			if (i == 0 || _E [i + 0] != _E [i - 2] || _E [i + 1] != _E [i - 1]) 
			{
				E[e*2+0]=_E [i + 0];
				E[e*2+1]=_E [i + 1];
				e++;
			}

		L = new float[E.Length/2];
		for (int e=0; e<E.Length/2; e++) 
		{
			int i = E[e*2+0];
			int j = E[e*2+1];
			L[e]=(X[i]-X[j]).magnitude;
		}

		V = new Vector3[X.Length];
		for (int i=0; i<X.Length; i++)
			V[i] = new Vector3 (0, 0, 0);
	}

	void Quick_Sort(ref int[] a, int l, int r)
	{
		int j;
		if(l<r)
		{
			j=Quick_Sort_Partition(ref a, l, r);
			Quick_Sort (ref a, l, j-1);
			Quick_Sort (ref a, j+1, r);
		}
	}

	int  Quick_Sort_Partition(ref int[] a, int l, int r)
	{
		int pivot_0, pivot_1, i, j;
		pivot_0 = a [l * 2 + 0];
		pivot_1 = a [l * 2 + 1];
		i = l;
		j = r + 1;
		while (true) 
		{
			do ++i; while( i<=r && (a[i*2]<pivot_0 || a[i*2]==pivot_0 && a[i*2+1]<=pivot_1));
			do --j; while(  a[j*2]>pivot_0 || a[j*2]==pivot_0 && a[j*2+1]> pivot_1);
			if(i>=j)	break;
			Swap(ref a[i*2], ref a[j*2]);
			Swap(ref a[i*2+1], ref a[j*2+1]);
		}
		Swap (ref a [l * 2 + 0], ref a [j * 2 + 0]);
		Swap (ref a [l * 2 + 1], ref a [j * 2 + 1]);
		return j;
	}

	void Swap(ref int a, ref int b)
	{
		int temp = a;
		a = b;
		b = temp;
	}

	private 
	
	void Strain_Limiting()
	{
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		Vector3[] vertices = mesh.vertices;
		
		Vector3[] sum_x = new Vector3[vertices.Length];
		int[] sum_n = new int[vertices.Length];
		for (int i = 0; i < vertices.Length; i++)
		{
			sum_x[i] = Vector3.zero;
			sum_n[i] = 0;
		}
		//Apply PBD here.
		//...
		
		for (int e=0; e<L.Length; e++) 
		{
			int vi = E[e*2+0];
			int vj = E[e*2+1];
		
			Vector3 xi_xj = vertices[vi] - vertices[vj];
			float len_ij = L[e] / xi_xj.magnitude;
			sum_x[vi] += 0.5f*(vertices[vi]+vertices[vj]+len_ij*xi_xj);
			sum_x[vj] += 0.5f*(vertices[vi]+vertices[vj]-len_ij*xi_xj);
			sum_n[vi] ++;
			sum_n[vj] ++;
		}
		
		for (int i = 0; i < vertices.Length; i++)
		{
			if(i==0||i==20) continue;
			Vector3 x_newTemp= (0.2f * vertices[i] + sum_x[i]) / (0.2f + sum_n[i]);
			V[i]+=(x_newTemp-vertices[i])/t;
			vertices[i] = x_newTemp;
		}
		
		// for(int i=0; i<vertices.Length; i++)
		// 		{
		// 			if (i == 0 || i == 20) continue;
		// 			V[i] += ((0.2f * vertices[i] + sum_x[i]) / (sum_n[i] + 0.2f) -
		// 			         vertices[i]) / t;
		// 			vertices[i] = (0.2f * vertices[i] + sum_x[i]) / (sum_n[i] + 0.2f);
		// 		}
		mesh.vertices = vertices;
		
	// 	Mesh mesh = GetComponent<MeshFilter> ().mesh;
	// 	Vector3[] vertices = mesh.vertices;
	//
	// 	//Apply PBD here.
	// 	Vector3[] sum_x = new Vector3[vertices.Length];
	// 	int[] num_x = new int[vertices.Length];
	// 	for(int i=0; i<vertices.Length; i++)
	// 	{
	// 		sum_x[i] = new Vector3(0, 0, 0);
	// 		num_x[i] = 0;
	// 	}
	//
	// 	for(int ei = 0; ei < L.Length; ei++)
	// 	{
	// 		int i = E[ei * 2];
	// 		int j = E[ei * 2 + 1];
	// 		Vector3 xi_xj = vertices[i] - vertices[j];
	// 		float len_ij = L[ei] / xi_xj.magnitude;
	//
	// 		sum_x[i] += (vertices[i] + vertices[j] + len_ij * (xi_xj)) / 2;
	// 		sum_x[j] += (vertices[i] + vertices[j] - len_ij * (xi_xj)) / 2;
	// 		num_x[i]++;
	// 		num_x[j]++;
	// 	}
	// 	for(int i=0; i<vertices.Length; i++)
	// 	{
	// 		if (i == 0 || i == 20) continue;
	// 		V[i] += ((0.2f * vertices[i] + sum_x[i]) / (num_x[i] + 0.2f) -
	// 		         vertices[i]) / t;
	// 		vertices[i] = (0.2f * vertices[i] + sum_x[i]) / (num_x[i] + 0.2f);
	// 	}
	// 	mesh.vertices = vertices;
	 }

	void Collision_Handling()
	{
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		Vector3[] X = mesh.vertices;
		
		//For every vertex, detect collision and apply impulse if needed.
		//...
		
		c = Sphere_go.transform.position;
		for (int i = 1; i < X.Length; i++)
		{
			if (i!=20)
			{
				if (Vector3.Magnitude(X[i]-c)<r)
				{
					V[i]+=(c+r*(X[i]-c)/(X[i]-c).magnitude-X[i])/t;
					X[i] = c + r * (X[i] - c) / (X[i] - c).magnitude;
				}
			}
		}
		mesh.vertices = X;
	}

	// Update is called once per frame
	void Update () 
	{
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		Vector3[] X = mesh.vertices;

		for(int i=0; i<X.Length; i++)
		{
			if(i==0 || i==20)	continue;
			//Initial Setup
			//...
			
			V[i] += Physics.gravity * t;
			V[i] *= damping;
			X[i] += t * V[i];
			
		}
		
		
		mesh.vertices = X;

		for(int l=0; l<32; l++)
			Strain_Limiting ();

		Collision_Handling ();
		
		mesh.RecalculateNormals ();

	}


}

