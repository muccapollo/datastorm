package com.datastorm.hackreativityandroid.widgets;

import android.content.Context;
import android.support.v7.widget.LinearLayoutManager;
import android.util.AttributeSet;
import android.view.View;

import com.dadino.quickstart.core.mvp.components.presenter.MvpView;
import com.datastorm.hackreativityandroid.R;
import com.datastorm.hackreativityandroid.adapters.AlertAdapter;
import com.datastorm.hackreativityandroid.interfaces.OnAlertListClickListener;
import com.datastorm.hackreativityandroid.mvp.entitites.Alert;
import com.datastorm.hackreativityandroid.mvp.usecases.alertlist.AlertListMVP;

import java.util.List;


public class PresentedAlertListView extends RecyclerLayout<AlertAdapter, LinearLayoutManager> {

	private AlertListMVP.Presenter mPresenter;
	private MvpView<List<Alert>> iView = new MvpView<>(this::onItemNext, this::onItemLoad);
	private OnAlertListClickListener listener;

	public PresentedAlertListView(Context context) {
		super(context);
	}

	public PresentedAlertListView(Context context, AttributeSet attrs) {
		super(context, attrs);
	}


	@Override
	protected void initialize() {
		setEmptyText(R.string.empty_alerts);
		setLayoutManager(new LinearLayoutManager(getContext()));
		final AlertAdapter adapter = new AlertAdapter();
		adapter.setOnAlertListClickListener(new OnAlertListClickListener() {
			@Override
			public void onMapShortcutClicked(View v) {
				if (listener != null) listener.onMapShortcutClicked(v);
			}

			@Override
			public void onRequestShortcutClicked(View v) {
				if (listener != null) listener.onRequestShortcutClicked(v);
			}

			@Override
			public void onReportShortcutClicked(View v) {
				if (listener != null) listener.onReportShortcutClicked(v);
			}

			@Override
			public void onAlertClicked(View v, Alert alert) {
				if (listener != null) listener.onAlertClicked(v, alert);
			}
		});
		setAdapter(adapter);
		setEnabled(false);
		addItemDecoration(new VerticalSpaceItemDecoration(
				getResources().getDimensionPixelSize(R.dimen.dimen_4dp)));
	}

	public void onItemNext(List<Alert> item) {
		mAdapter.setItems(item);
	}


	public void onItemLoad(boolean loading) {
		setListLoading(loading);
	}

	public void setOnAlertClickedListener(OnAlertListClickListener listener) {
		this.listener = listener;
	}

	public void setPresenter(AlertListMVP.Presenter presenter) {
		if (mPresenter != null) mPresenter.removeView(iView);
		mPresenter = presenter;
		if (mPresenter != null) mPresenter.addView(iView);
	}
}
