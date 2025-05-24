namespace BirileriWebSitesi.Helpers
{
    public class CookieHelper
    {
        string MyCart { get; set; } = string.Empty;
        public Dictionary<int, string> AddBasketCookie(string cookie, string productCode, decimal quantity)
        {
            try
            {

                var cookieOptions = new CookieOptions();
                //returns result as int different product count in basket and cookie as string
                Dictionary<int, string> result = new();
                cookieOptions = new CookieOptions();
                cookieOptions.Expires = DateTime.Now.AddDays(30);
                cookieOptions.Path = "/";

                if (cookie == null)
                {

                    result = UpdateCookieAddProduct(productCode, quantity, string.Empty);
                    if (result.Values.FirstOrDefault() == "HATA")
                    {
                        return result;
                    }

                    HttpContext.Response.Cookies.Append("MyCart", result.Values.FirstOrDefault(), cookieOptions);
                    return result;

                }
                else
                {
                    result = UpdateCookieAddProduct(productCode, quantity, cookie);

                    if (result.Values.FirstOrDefault() == "HATA")
                    {
                        return result;
                    }
                    HttpContext.Response.Cookies.Append("MyCart", result.Values.FirstOrDefault(), cookieOptions);

                    return result;

                }

            }
            catch (Exception)
            {
                Dictionary<int, string> result = new();
                result.Add(0, "HATA");
                return result; ;
            }
        }
        public Dictionary<int, string> UpdateCookie(string productCode, decimal quantity, string cookie)
        {
            try
            {
                bool exists = false;
                //how many items in cookie,cookie
                Dictionary<int, string> result = new();
                if (cookie == "")
                {
                    MyCart = productCode + ";" + quantity.ToString();
                    result.Add(1, MyCart);
                    return result;
                }
                else
                {
                    string[] MyCartArray = cookie.Split('&');
                    string updatedExistingID = string.Empty;
                    bool firstItem = true;
                    int totalProductCount = 0;
                    foreach (string item in MyCartArray)
                    {
                        string[] existingID = item.Split(';');

                        if (existingID[0] == productCode)
                        {
                            exists = true;
                            existingID[1] = quantity.ToString();
                        }
                        //eðer ilk ürün ise & ekleme
                        if (firstItem)
                            updatedExistingID = updatedExistingID + string.Join(";", existingID);
                        else
                            updatedExistingID = updatedExistingID + "&" + string.Join(";", existingID);

                        firstItem = false;
                    }
                    if (!exists)
                    {
                        totalProductCount = MyCartArray.Count() + 1;
                        MyCart = cookie + "&" + productCode + ";" + quantity.ToString();
                        result.Add(totalProductCount, MyCart);
                        return result;
                    }

                    else
                    {
                        totalProductCount = MyCartArray.Count();
                        MyCart = updatedExistingID;
                        result.Add(totalProductCount, MyCart);
                        return result;
                    }
                }
            }
            catch (Exception)
            {
                Dictionary<int, string> result = new();
                result.Add(0, "HATA");
                return result;
            }
        }
        public Dictionary<int, string> UpdateCookieAddProduct(string productCode, decimal quantity, string cookie)
        {
            try
            {
                string MyCart = string.Empty;
                bool exists = false;
                //how many items in cookie,cookie
                Dictionary<int, string> result = new();
                if (cookie == "")
                {
                    MyCart = productCode + ";" + quantity.ToString();
                    result.Add(1, MyCart);
                    return result;
                }
                else
                {
                    string[] MyCartArray = cookie.Split('&');
                    string updatedExistingID = string.Empty;
                    bool firstItem = true;
                    int totalProductCount = 0;
                    foreach (string item in MyCartArray)
                    {
                        string[] existingID = item.Split(';');

                        if (existingID[0] == productCode)
                        {
                            exists = true;
                            existingID[1] = (Convert.ToDecimal(existingID[1]) + quantity).ToString();
                        }
                        //eðer ilk ürün ise & ekleme
                        if (firstItem)
                            updatedExistingID = updatedExistingID + string.Join(";", existingID);
                        else
                            updatedExistingID = updatedExistingID + "&" + string.Join(";", existingID);

                        firstItem = false;
                    }
                    if (!exists)
                    {
                        totalProductCount = MyCartArray.Count() + 1;
                        MyCart = cookie + "&" + productCode + ";" + quantity.ToString();
                        result.Add(totalProductCount, MyCart);
                        return result;
                    }

                    else
                    {
                        totalProductCount = MyCartArray.Count();
                        MyCart = updatedExistingID;
                        result.Add(totalProductCount, MyCart);
                        return result;
                    }
                }
            }
            catch (Exception)
            {
                Dictionary<int, string> result = new();
                result.Add(0, "HATA");
                return result;
            }
        }
        public Dictionary<int, string> RemoveCookie(string productCode, string cookie)
        {
            try
            {
                bool found = false;
                //how many items in cookie,cookie
                Dictionary<int, string> result = new();

                string[] MyCartArray = cookie.Split('&');
                string removedVersion = string.Empty;
                bool firstItem = true;
                int totalProductCount = 0;
                foreach (string item in MyCartArray)
                {
                    string[] existingID = item.Split(';');

                    if (existingID[0] == productCode)
                        found = true;

                    //eðer ilk ürün ise & ekleme
                    if (firstItem && !found)
                        removedVersion = removedVersion + string.Join(";", existingID);
                    else if (string.IsNullOrEmpty(removedVersion) && !found)
                        removedVersion = string.Join(";", existingID);
                    else if (!found)
                        removedVersion = removedVersion + "&" + string.Join(";", existingID);

                    firstItem = false;
                    found = false;
                }

                totalProductCount = MyCartArray.Count() - 1;
                result.Add(totalProductCount, removedVersion);
                return result;

            }
            catch (Exception)
            {
                //returns result as int different product count in basket and cookie as string
                Dictionary<int, string> result = new();
                result.Add(0, "HATA");
                return result;
            }
        }
        public Dictionary<string, int> GetProductsFromCookie(string cookie)
        {
            try
            {
                string[] MyCartArray = cookie.Split('&');
                Dictionary<string, int> result = new();
                string productCode = string.Empty;
                int quantity = 0;
                foreach (string item in MyCartArray)
                {
                    string[] existingID = item.Split(';');

                    if (string.IsNullOrEmpty(existingID[0]))
                        productCode = string.Empty;
                    else
                        productCode = existingID[0];
                    if (Int32.TryParse(existingID[1], out quantity) == false)
                        quantity = 0;
                    else
                        quantity = Convert.ToInt32(existingID[1]);

                    result.Add(productCode, quantity);

                }
                return result;

            }
            catch (Exception)
            {
                //returns result as int different product count in basket and cookie as string
                Dictionary<string, int> result = new();
                result.Add("HATA", 0);
                return result;
            }
        }
    }
}
