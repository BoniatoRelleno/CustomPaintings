namespace CustomPainting;

public static class Extensions
{
    public static bool IsPainting(this GrabbableObject obj)
    {
        return obj.itemProperties.itemName == "Painting";
    }
}
