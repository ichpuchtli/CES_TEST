namespace CES_TEST;

public partial class MainPage : ContentPage
{
	public MainPage(MainPageViewModel vm)
	{
		InitializeComponent();
		BindingContext = vm;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		(BindingContext as MainPageViewModel)?.PageAppearing();
	}
}