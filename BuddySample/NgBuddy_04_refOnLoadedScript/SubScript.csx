
#r "System.Drawing.dll"

void CheckColor()
{
    var c = System.Drawing.Color.FromArgb(255, 0, 0, 0);
    Api.Log($"Color: {c.A}");
}
