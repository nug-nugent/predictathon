function Highlight(obj, newClassName) {
    if(obj != null)
    {
        obj.originalClassName = obj.className;
        obj.className = newClassName;
    }
} 

function Highlight_Off(obj) {
    if(obj != null) {
        obj.className = obj.originalClassName;
    }
}


function IsNumeric(value) {
    return !isNaN(parseFloat(value)) && isFinite(value);
}

function ResetScrollPosition() {
    var scrollX = document.getElementById('__SCROLLPOSITIONX');
    var scrollY = document.getElementById('__SCROLLPOSITIONY');

    if (scrollX && scrollY) {
        scrollX.value = 0;
        scrollY.value = 0;
    }
} 