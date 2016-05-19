


function getJson(url, callback){
  ajaxJson(url, 'get', {}, callback);
}

function getJson(url, jsonData, callback){
  ajaxJson(url, 'get', jsonData, callback);
}

function postJson(url, jsonData, callback)
{
  ajaxJson(url,'post', jsonData, callback);
}

function deleteJson(url, jsonData, callback)
{
  ajaxJson(url,'delete', jsonData, callback);
}

function ajaxJson(url, method, jsonData, callback){
  var json = JSON.stringify(jsonData);
  $.ajax({
    type:method,
    url:url,
    data : json,
    dataType : 'JSON',
    scriptCharset: 'utf-8',
    success : callback,
    error : function(data) {
        // Error
        alert("error");
        alert(JSON.stringify(data));
    }
  });
}

function readFile(file, callback)
{
  var isEnable=file.files && file.files[0];
  if ( isEnable == undefined ) {
    callback();
  }

  var r = new FileReader();
  r.onload = function(e) {
    var base64String = e.target.result;
    callback(base64String.split(',')[1]);
  };
  r.readAsDataURL( file.files[0] );
}

function clearTypeahead()
{
    localStorage.removeItem("__names__adjacencyList");
    localStorage.removeItem("__names__adjacencyList__ttl__");
    localStorage.removeItem("__names__itemHash");
    localStorage.removeItem("__names__itemHash__ttl__");
    localStorage.removeItem("__names__protocol");
    localStorage.removeItem("__names__protocol__ttl__");
    localStorage.removeItem("__names__thumbprint");
    localStorage.removeItem("__names__thumbprint__ttl__");
}
