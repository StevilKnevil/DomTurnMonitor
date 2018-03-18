using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace DomTurnMonitor
{


  public partial class MainPage : ContentPage
	{
    public class Employee
    {
      public string DisplayName { get; set; }
    }

    public ObservableCollection<Employee> employees = new ObservableCollection<Employee>();

    public MainPage()
		{
      InitializeComponent();

      employees.Add(new Employee { DisplayName = "Rob Finnerty" });
      employees.Add(new Employee { DisplayName = "Bill Wrestler" });
      employees.Add(new Employee { DisplayName = "Dr. Geri-Beth Hooper" });
      employees.Add(new Employee { DisplayName = "Dr. Keith Joyce-Purdy" });
      employees.Add(new Employee { DisplayName = "Sheri Spruce" });
      employees.Add(new Employee { DisplayName = "Burt Indybrick" });

      //empList.ItemsSource = employees;
    }

    private void btn_Click(object sender, EventArgs e)
    {
    }
	}
}
