package com.datastorm.hackreativityandroid.interfaces;


import com.dadino.quickstart.core.interfaces.IRepository;
import com.datastorm.hackreativityandroid.mvp.entitites.MapObject;

import java.util.List;

import rx.Observable;
import rx.Single;

public interface IMapObjectRepository extends IRepository {

	Observable<List<MapObject>> retrieve(String topic);
	Single<Boolean> update(List<MapObject> mapObjects);
}
