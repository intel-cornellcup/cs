﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CarControl : MonoBehaviour {

	public bool manual = true;
	//the wheel colliders for the car
	public Transform[] wheels;
	//the rigidbody of the car
	private Rigidbody rb; 

	//These are gear ratios that are the ratios to apply the torque to the wheels for maximum realism
	public float[] GearRatio;
	public int CurrGear = 0;

	//the input power,steer, and RPM to the vehicle
	private float input_torque;
	private float input_steer;
	private float input_rpm;
	private float brake_power; 

	//the maximum steering sharpness for the car
	public float maxSteer = 12.0f;
	//the engine power 
	public float engineTorque = 150.0f;
	//the brake torque
	public float brakeTorque = 30.0f;

	//the maximum and minimum rpm for the engine
	public float maxEngineRPM = 3000.0f;
	public float minEngineRPM = 1000.0f;

	// Objects holding the way points for car movement
	public GameObject wayPointObject;
	private List<Transform> wayPoints; 
	private int current_point = 0;

	//initialize center of mass to prevent car from flipping over too much, as well as getting way points from in game object
	void Start () {
		rb = GetComponent<Rigidbody>();
		rb.centerOfMass = new Vector3(0.0f,-0.3f,0.7f);
		GetWayPoints ();
	}

	//allows for manual drive or AI pathfinding
	void FixedUpdate() {
		if (manual) 
			Manual_Update ();
		else 
			AI_Update ();
	}

	//code for self-driving of car/debugging purposes 
	void Manual_Update() {
		input_torque = Input.GetAxis("Vertical") * engineTorque * Time.deltaTime * 250.0f;
		input_steer = Input.GetAxis("Horizontal") * maxSteer;

		GetCollider(0).steerAngle = input_steer;
		GetCollider(1).steerAngle = input_steer;
		GetCollider(0).motorTorque = input_torque;
		GetCollider(1).motorTorque = input_torque;
		GetCollider(2).motorTorque = input_torque;
		GetCollider(3).motorTorque = input_torque;
	}

	//car driving around the waypoints
	void AI_Update () {
		rb.drag = rb.velocity.magnitude / 250;
		GoToWayPoint ();

		input_rpm = (GetCollider (0).rpm + GetCollider (1).rpm) / 2 * GearRatio [CurrGear];
		ShiftGears ();

		GetCollider (0).motorTorque = engineTorque / GearRatio [CurrGear] * input_torque;
		GetCollider (1).motorTorque = engineTorque / GearRatio [CurrGear] * input_torque;
		GetCollider (2).motorTorque = engineTorque / GearRatio [CurrGear] * input_torque;
		GetCollider (3).motorTorque = engineTorque / GearRatio [CurrGear] * input_torque;
		GetCollider (0).brakeTorque = brake_power;
		GetCollider (1).brakeTorque = brake_power;
		GetCollider (2).brakeTorque = brake_power;
		GetCollider (3).brakeTorque = brake_power;

		GetCollider (0).steerAngle = maxSteer * input_steer;
		GetCollider (1).steerAngle = maxSteer * input_steer;
	}

	//get the way points from the in game object
	private void GetWayPoints() {
		Transform[] tenativeWayPoints = wayPointObject.GetComponentsInChildren<Transform> ();
		wayPoints = new List<Transform> ();

		foreach (Transform point in tenativeWayPoints) {
			if (point.transform != wayPointObject.transform) 
				wayPoints.Add(point);
		}
	}

	private void ShiftGears() {
		if (input_rpm >= maxEngineRPM) {
			for (int i = 0; i < GearRatio.Length; i++) {
				if (GetCollider (0).rpm * GearRatio[i] < maxEngineRPM) {
					CurrGear = i;
					break;
				}
			}
		}

		if (input_rpm <= minEngineRPM) {
			for (int i = 0; i < GearRatio.Length; i++) {
				if (GetCollider (0).rpm * GearRatio[i] > minEngineRPM) {
					CurrGear = i;
					break; 
				}
			}
		}
	}

	//set input_steer and input_torque to move towards the way point 
	private void GoToWayPoint() {
		Vector3 travelDirection = transform.InverseTransformPoint(new Vector3 (wayPoints[current_point].position.x, transform.position.y, wayPoints[current_point].position.z));

		input_steer = travelDirection.x / travelDirection.magnitude;

		if (input_steer < 0.35f && input_steer > -0.35f) {
			input_torque = travelDirection.z / travelDirection.magnitude;
		} else {
			input_torque = travelDirection.z / travelDirection.magnitude - Mathf.Abs (input_steer);
		}


		if (travelDirection.magnitude < 10) {
			current_point ++;

			if (current_point >= wayPoints.Count) {
				current_point = 0;
			}
		}

		int next_point = current_point + 1;
		if (next_point >= wayPoints.Count)
			next_point = 0;
		Vector3 nextDirection = transform.InverseTransformPoint (new Vector3 (wayPoints [next_point].position.x, transform.position.y, wayPoints [next_point].position.z));
		float angle = Vector3.Angle (travelDirection, nextDirection);

		if (angle > 90.0f && rb.velocity.magnitude > 5.0f) {
			brake_power = brakeTorque;
			input_torque = input_torque/4;
		} else {
			brake_power = 0.0f;
		}

	}

	//get each of the wheels
	private WheelCollider GetCollider(int n) {
		return wheels[n].gameObject.GetComponent<WheelCollider>();
	}

}