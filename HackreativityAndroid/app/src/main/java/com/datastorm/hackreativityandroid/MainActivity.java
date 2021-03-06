package com.datastorm.hackreativityandroid;

import android.accounts.Account;
import android.accounts.AccountManager;
import android.content.Intent;
import android.content.res.Configuration;
import android.os.Bundle;
import android.support.design.widget.NavigationView;
import android.support.v4.app.Fragment;
import android.support.v4.app.FragmentManager;
import android.support.v4.app.FragmentTransaction;
import android.support.v4.view.GravityCompat;
import android.support.v4.widget.DrawerLayout;
import android.support.v7.app.ActionBarDrawerToggle;
import android.text.TextUtils;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.widget.TextView;

import com.dadino.quickstart.core.BaseActivity;
import com.dadino.quickstart.core.interfaces.DrawerToggleServer;
import com.dadino.quickstart.core.mvp.components.presenter.MvpView;
import com.dadino.quickstart.core.mvp.components.presenter.PresenterManager;
import com.dadino.quickstart.core.utils.Logs;
import com.dadino.quickstart.login.mvp.components.Authenticator;
import com.dadino.quickstart.login.mvp.usecases.lastusedaccount.LastAccountMVP;
import com.datastorm.hackreativityandroid.fragments.MapObjectListFragment;
import com.datastorm.hackreativityandroid.fragments.ReportFragment;
import com.datastorm.hackreativityandroid.fragments.RequestListFragment;
import com.datastorm.hackreativityandroid.fragments.TopicListFragment;
import com.datastorm.hackreativityandroid.interfaces.OnAlertListInteractionListener;
import com.datastorm.hackreativityandroid.interfaces.OnMapObjectListInteractionListener;
import com.datastorm.hackreativityandroid.interfaces.OnRequestListInteractionListener;
import com.datastorm.hackreativityandroid.interfaces.OnTopicAddInteractionListener;
import com.datastorm.hackreativityandroid.interfaces.OnTopicListInteractionListener;
import com.datastorm.hackreativityandroid.mvp.components.ErrorHandler;
import com.datastorm.hackreativityandroid.mvp.entitites.Alert;
import com.datastorm.hackreativityandroid.utils.Seeder;

import butterknife.BindView;

import static butterknife.ButterKnife.bind;

public class MainActivity extends BaseActivity implements NavigationView
		.OnNavigationItemSelectedListener,
		DrawerToggleServer,
		OnAlertListInteractionListener,
		OnRequestListInteractionListener,
		OnMapObjectListInteractionListener,
		OnTopicListInteractionListener,
		OnTopicAddInteractionListener {

	public static final  String MAIN_FRAGMENT          = "main_fragment";
	private static final String LAST_CLICKED_NAV_ITEM  = "last_clicked_nav_item";
	private static final int    ACCOUNT_PICKER_REQUEST = 1234;
	private static final String LAST_ACCOUNT           = "chosen_account";

	@BindView(R.id.nav_view)
	NavigationView navigationView;
	@BindView(R.id.drawer_layout)
	DrawerLayout   mDrawer;

	private TextView              navHeaderEmail;
	private ActionBarDrawerToggle mDrawerToggle;

	private PresenterManager<LastAccountMVP.Presenter> accountPresenterManager;
	private String                                     mChosenAccount;
	private MvpView<String> iAccountView       = new MvpView<>(this::onAccountLoaded,
			this::onAccountError, this);
	private int             lastClickedNavItem = -1;
	private int             requestedNavItemId = -1;


	@Override
	protected void onCreate(Bundle savedInstanceState) {
		super.onCreate(savedInstanceState);
		setContentView(R.layout.activity_main);
		bind(this);

		navigationView.setNavigationItemSelectedListener(this);
		navigationView.getHeaderView(0)
		              .setOnClickListener(view -> pickAccount());
		navHeaderEmail = (TextView) navigationView.getHeaderView(0)
		                                          .findViewById(R.id.nav_header_email);

		initPresenters();

		if (savedInstanceState != null) {
			requestedNavItemId = savedInstanceState.getInt(LAST_CLICKED_NAV_ITEM);
			lastClickedNavItem = savedInstanceState.getInt(LAST_CLICKED_NAV_ITEM);
			mChosenAccount = savedInstanceState.getString(LAST_ACCOUNT);
			MenuItem item = navigationView.getMenu()
			                              .findItem(requestedNavItemId);
			if (item != null) onNavigationItemSelected(item, false);

			getSupportFragmentManager().getFragment(savedInstanceState, MAIN_FRAGMENT);
		} else {
			MenuItem item = navigationView.getMenu()
			                              .findItem(R.id.action_show_alert_list);
			if (item != null) onNavigationItemSelected(item, false);
		}
	}

	private void initPresenters() {
		accountPresenterManager = new PresenterManager<>(this, new LastAccountMVP.Factory(),
				LastAccountMVP.Presenter::onAccountRequested).bindTo(this);
	}

	@Override
	protected void onPostCreate(Bundle savedInstanceState) {
		super.onPostCreate(savedInstanceState);
		if (mDrawerToggle != null) mDrawerToggle.syncState();
	}

	@Override
	public void onConfigurationChanged(Configuration newConfig) {
		super.onConfigurationChanged(newConfig);
		if (mDrawerToggle != null) mDrawerToggle.onConfigurationChanged(newConfig);
	}

	@Override
	protected void onStart() {
		super.onStart();
		if (accountPresenter() != null) accountPresenter().addView(iAccountView);
	}

	@Override
	protected void onStop() {
		if (accountPresenter() != null) accountPresenter().removeView(iAccountView);
		super.onStop();
	}

	@Override
	public void onSaveInstanceState(Bundle outState) {
		super.onSaveInstanceState(outState);
		outState.putInt(LAST_CLICKED_NAV_ITEM, lastClickedNavItem);
		outState.putString(LAST_ACCOUNT, mChosenAccount);
		final FragmentManager manager = getSupportFragmentManager();
		final Fragment main = manager.findFragmentById(R.id.fragment_container);

		if (main != null) manager.putFragment(outState, MAIN_FRAGMENT, main);
	}

	@Override
	public boolean onCreateOptionsMenu(Menu menu) {
		getMenuInflater().inflate(R.menu.main, menu);
		return true;
	}

	@Override
	public boolean onOptionsItemSelected(MenuItem item) {
		if (mDrawerToggle.onOptionsItemSelected(item)) return true;
		switch (item.getItemId()) {
			case R.id.action_seed:
				Seeder.seed(this);
				return true;
		}
		return super.onOptionsItemSelected(item);
	}

	@Override
	public boolean onNavigationItemSelected(MenuItem item) {
		return onNavigationItemSelected(item, true);
	}

	public boolean onNavigationItemSelected(MenuItem item, boolean showFragment) {
		Logs.ui("Clicked item " + item.getItemId() + " " + item.getTitle());

		DrawerLayout drawer = (DrawerLayout) findViewById(R.id.drawer_layout);
		drawer.closeDrawer(GravityCompat.START);
		if (checkAndPersistNavItem(item.getItemId())) openFragment(item.getItemId());
		return true;
	}

	private void openFragment(int itemId) {
		switch (itemId) {
			case R.id.action_show_alert_list:
				getSupportFragmentManager().beginTransaction()
				                           .replace(R.id.fragment_container,
						                           TopicListFragment.newInstance(),
						                           TopicListFragment.TAG)
				                           .commitNow();
				break;
			case R.id.action_show_report:
				getSupportFragmentManager().beginTransaction()
				                           .replace(R.id.fragment_container,
						                           ReportFragment.newInstance(), ReportFragment
								                           .TAG)
				                           .commitNow();
				break;
			case R.id.action_show_request_lists:
				getSupportFragmentManager().beginTransaction()
				                           .replace(R.id.fragment_container,
						                           RequestListFragment.newInstance(),
						                           RequestListFragment.TAG)
				                           .commitNow();
				break;
			case R.id.action_show_map:
				getSupportFragmentManager().beginTransaction()
				                           .replace(R.id.fragment_container,
						                           MapObjectListFragment.newInstance(),
						                           MapObjectListFragment.TAG)
				                           .commitNow();
				break;
		}
	}

	private boolean checkAndPersistNavItem(int itemId) {
		navigationView.setCheckedItem(itemId);
		lastClickedNavItem = itemId;
		if (lastClickedNavItem == requestedNavItemId) requestedNavItemId = -1;
		return true;
	}


	private void pickAccount() {
		Account account = null;
		if (!TextUtils.isEmpty(mChosenAccount)) account = new Account(mChosenAccount,
				Authenticator.IT_LAMINOX_AUTH_EMAIL);
		Intent intent = AccountManager.newChooseAccountIntent(account, null,
				new String[]{Authenticator.IT_LAMINOX_AUTH_EMAIL}, true, null, null, null, null);
		startActivityForResult(intent, ACCOUNT_PICKER_REQUEST);
	}

	protected void onActivityResult(final int requestCode, final int resultCode,
	                                final Intent data) {
		if (requestCode == ACCOUNT_PICKER_REQUEST && resultCode == RESULT_OK) {
			String accountName = data.getStringExtra(AccountManager.KEY_ACCOUNT_NAME);
			if (accountPresenter() != null) accountPresenter().onAccountSelected(accountName);
		}
	}

	@Override
	public void onBackPressed() {
		DrawerLayout drawer = (DrawerLayout) findViewById(R.id.drawer_layout);
		if (drawer.isDrawerOpen(GravityCompat.END)) {
			drawer.closeDrawer(GravityCompat.END);
		} else if (drawer.isDrawerOpen(GravityCompat.START)) {
			drawer.closeDrawer(GravityCompat.START);
		} else {
			super.onBackPressed();
		}
	}

	@Override
	protected void onPause() {
		super.onPause();
		navigationView.setNavigationItemSelectedListener(null);
		mDrawer.removeDrawerListener(mDrawerToggle);
	}

	@Override
	protected void onResume() {
		super.onResume();
		navigationView.setNavigationItemSelectedListener(this);
		mDrawer.addDrawerListener(mDrawerToggle);
	}


	private void onAccountLoaded(String accountName) {
		boolean accountChanged = mChosenAccount == null || !mChosenAccount.equals(accountName);
		Logs.model("Account selected: " + accountName);
		navHeaderEmail.setText(accountName);
		mChosenAccount = accountName;
		if (TextUtils.isEmpty(mChosenAccount)) {
			//TODO LoginActivity.showForResult(this);
			return;
		}

		if (accountChanged) {
			removeFragments();
			//TODO open last selected fragment

			//Crashlytics.setUserIdentifier(accountName);
			//Crashlytics.setUserEmail(accountName);
			//Crashlytics.setUserName(accountName);
		}
	}

	private void removeFragments() {
		final Fragment oldFragment = getSupportFragmentManager().findFragmentById(
				R.id.fragment_container);
		final FragmentTransaction transaction = getSupportFragmentManager().beginTransaction();
		if (oldFragment != null) transaction.remove(oldFragment);
		transaction.commit();
	}

	private void onAccountError(Throwable throwable) {
		ErrorHandler.analyzeError(throwable);
		//TODO show something?
	}


	private LastAccountMVP.Presenter accountPresenter() {
		return accountPresenterManager.get();
	}

	@Override
	public DrawerLayout getDrawer() {
		return mDrawer;
	}

	@Override
	public void setDrawerToggle(ActionBarDrawerToggle toggle) {
		mDrawer.removeDrawerListener(mDrawerToggle);
		mDrawerToggle = toggle;
		if (mDrawerToggle != null) {
			mDrawer.addDrawerListener(mDrawerToggle);
			mDrawerToggle.syncState();
		}
	}

	@Override
	public void removeDrawerToggle() {
		mDrawer.removeDrawerListener(mDrawerToggle);
	}

	@Override
	public void onAlertClicked(View view, Alert alert) {
		//TODO
	}

	@Override
	public void onMapShortcutClicked(View v) {
		MenuItem item = navigationView.getMenu()
		                              .findItem(R.id.action_show_map);
		if (item != null) onNavigationItemSelected(item, false);
	}

	@Override
	public void onRequestShortcutClicked(View v) {
		MenuItem item = navigationView.getMenu()
		                              .findItem(R.id.action_show_request_lists);
		if (item != null) onNavigationItemSelected(item, false);
	}

	@Override
	public void onReportShortcutClicked(View v) {
		MenuItem item = navigationView.getMenu()
		                              .findItem(R.id.action_show_report);
		if (item != null) onNavigationItemSelected(item, false);
	}

	@Override
	public void onNewRequestClicked() {
		RequestNewActivity.show(this);
	}
}
