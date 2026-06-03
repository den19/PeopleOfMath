import re
import urllib.request

def get(url):
    req = urllib.request.Request(url, headers={"User-Agent": "PeopleOfMath/1.0"})
    return urllib.request.urlopen(req).read().decode()

url = (
    "https://commons.wikimedia.org/w/api.php?action=query&generator=search"
    "&gsrnamespace=6&gsrsearch=Isaac%20Newton%20portrait&gsrlimit=5"
    "&prop=imageinfo&iiprop=url|extmetadata&format=json"
)
j = get(url)
print("File titles:", re.findall(r'"title":"(File:[^"]+)"', j)[:5])
print("License:", re.search(r'LicenseShortName.*?value":"([^"]+)"', j))
print("URL:", re.search(r'"url":"(https://upload[^"]+)"', j))

wd = get("https://www.wikidata.org/w/api.php?action=wbgetentities&ids=Q935&props=claims&format=json")
m = re.search(r'"P18".*?"datavalue".*?"value":"([^"]+)"', wd)
print("P18:", m.group(1) if m else None)
