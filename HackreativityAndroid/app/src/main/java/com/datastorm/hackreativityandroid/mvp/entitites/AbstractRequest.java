package com.datastorm.hackreativityandroid.mvp.entitites;


import com.google.gson.annotations.Expose;
import com.google.gson.annotations.SerializedName;

import java.util.Date;

import io.requery.Column;
import io.requery.Entity;
import io.requery.Generated;
import io.requery.Key;
import io.requery.Table;

@Entity(stateless = true)
@Table(name = "request")
public class AbstractRequest {

	@Expose
	@SerializedName("Id")
	@Column(name = "id")
	@Key
	@Generated
	long id;

	@Expose
	@SerializedName("ServerId")
	@Column(name = "server_id")
	@Key
	@Generated
	long serverId;

	@Expose
	@SerializedName("Indirizzo")
	@Column(name = "address")
	String address;

	@Expose
	@SerializedName("Tipo")
	@Column(name = "type")
	int type;

	@Expose
	@SerializedName("DataRichiesta")
	@Column(name = "requested_at")
	Date requestedAt;

	@Expose
	@SerializedName("CondizionePorta")
	@Column(name = "condition_door")
	boolean conditionDoor;

	@Expose
	@SerializedName("CondizioneScale")
	@Column(name = "condition_stairs")
	boolean conditionStairs;
}
