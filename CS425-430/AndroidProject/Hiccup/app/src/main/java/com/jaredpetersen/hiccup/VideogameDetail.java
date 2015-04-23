package com.jaredpetersen.hiccup;

import android.content.Intent;
import android.os.Bundle;
import android.support.v7.app.ActionBarActivity;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import java.text.ParseException;
import java.text.SimpleDateFormat;

/**
 * Created by jaredpetersen on 4/12/15.
 */

public class VideogameDetail extends ActionBarActivity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        // Tell the activity which XML layout is right
        setContentView(R.layout.videogame_detail);

        // Enable the "Up" button for more navigation options
        getSupportActionBar().setDisplayHomeAsUpEnabled(true);

        String gameTitle = this.getIntent().getExtras().getString("title");
        String consoleName = this.getIntent().getExtras().getString("consoleName");
        String releaseDate = this.getIntent().getExtras().getString("releaseDate");

        // Need to convert release date format
        SimpleDateFormat databaseFormat = new SimpleDateFormat("yyyy-MM-dd");
        SimpleDateFormat displayFormat = new SimpleDateFormat("MM/dd/yyyy");

        try
        {
            releaseDate = displayFormat.format(databaseFormat.parse(releaseDate));
        }
        catch (ParseException e)
        {
            e.printStackTrace();
        }

        String esrb = this.getIntent().getExtras().getString("esrb");

        TextView titleTV = (TextView) findViewById(R.id.game_title);
        titleTV.setText(gameTitle);

        TextView consoleTV = (TextView) findViewById(R.id.game_console);
        consoleTV.setText(consoleName);

        TextView releaseTV = (TextView) findViewById(R.id.release_date);
        releaseTV.setText(releaseDate);

        TextView esrbTV = (TextView) findViewById(R.id.esrb);
        esrbTV.setText(esrb);

        // Access the imageview from XML
        //ImageView imageView = (ImageView) findViewById(R.id.img_cover);
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu)
    {
        // Inflate the menu; this adds items to the action bar if it is present.
        getMenuInflater().inflate(R.menu.menu_main, menu);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item)
    {
        // Handle action bar item clicks here. The action bar will
        // automatically handle clicks on the Home/Up button, so long
        // as you specify a parent activity in AndroidManifest.xml.
        int id = item.getItemId();

        //noinspection SimplifiableIfStatement
        if (id == R.id.action_settings) {
            return true;
        }
        else if (id == R.id.action_logout) {
            // This will delete the stored credentials and return to the login activity
            finish();
            Intent i = new Intent(VideogameDetail.this, LoginActivity.class);
            startActivity(i);
            return true;
        }

        return super.onOptionsItemSelected(item);
    }

}
