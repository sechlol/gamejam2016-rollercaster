﻿using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;
using System;

public enum Side{LEFT, RIGHT}

public class NetworkPlayer : NetworkBehaviour {

	private Side side;
	public static NetworkPlayer current{get;private set;}
	public event Action OnReady;
	public NetworkSpell[] spells;
	public float[] Joystick;

	public override void OnStartLocalPlayer(){
		base.OnStartLocalPlayer ();

		if(!hasAuthority)
			return;
		
		current = this;
		GameObject wheels = GameObject.FindWithTag("Wheels");
		wheels.SetActive(true);
		NetworkId id = GetComponent<NetworkId>();
		id.AddInitializeListener(()=>{
			Utils.Log("Player "+name+" ready");	
		});
	}
		
	void Start () {
		if(transform.position.x < 0)
			side = Side.LEFT;
		else
			side = Side.RIGHT;

		if(isServer){
			spells = new NetworkSpell[4];
			Joystick = new float[4];
		}
	}

	[Client]
	public void CastSpell(SpellType type, int level, int id){
		Utils.Log(name+" cast "+type+" "+level+" on "+(isServer ? "server" : "client"));
		CmdSpawnSpell(type, level, id);
	}


	[Command]
	public void CmdSpawnSpell(SpellType type, int level, int id){
		Utils.Log("Player "+name+" cast spell");

		//get spell and assign id
		Spell s = SpellDB.GetSpell(type, level);
		s.id = id;

		//instantiate acual spell
		GameObject go = Instantiate(s.prefab, transform.position, Quaternion.identity) as GameObject;
		go.name = name+"spell"+id;

		NetworkSpell nSpell = go.GetComponent<NetworkSpell>();
		nSpell.SetSpell(s, side);
		nSpell.Spell.owner = this;

		nSpell.OnDestroy += (NetworkSpell obj) => {
			Spell sp = obj.Spell;
			Joystick[sp.id] = 0;
			spells[sp.id] = null;

			RpcSpellDestroyed(sp.id, sp.level);
		};

		//RpcSpellCasted(go.tag);
	}
		
	[ClientRpc]
	void RpcSpellDestroyed(int id, int level){
		GameObject obj =  GameObject.FindWithTag("Wheel"+id);
		Wheel w = obj.GetComponent<Wheel>();

	}

	[Client]
	public void Move(int id, float v){
		CmdUpdateSpellVertical(id, v);
	}

	[Command]
	public void CmdUpdateSpellVertical(int id, float v){
		Joystick[id] = v;
	}
	/*

	void Update () {
		if(!isLocalPlayer)
			return;

		if(_canCast && Input.GetKeyDown(KeyCode.Space)){
			Spell s = SpellDB.GetRandom();
			s.level = Random.Range(1,4);
			s.id = 1;

			CastSpell(s);
		}
	}*/
}
