namespace LilyGoTestApp;

[Activity(Label = "ActivitySelected")]
public class ActivitySelected : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Create your application here
        SetContentView(Resource.Layout.selected_device);
    }
}