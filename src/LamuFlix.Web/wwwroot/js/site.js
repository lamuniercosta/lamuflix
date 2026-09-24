// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function AssistirFilme(id) {
    $.ajax(
        {
            type: 'GET',
            url: '/Filmes/AssistirFilme',
            data: { id: id },
            dataType: 'html',
            cache: false,
            async: true,
            error: function (data) {
                alert(data);
            }
        });
}
$(document).ready(function () {
    $('#SearchField').autocomplete({
        source: function (request, response) {
            $.getJSON("/Filmes/GetFilmesJson?query=" + request.term, function (data) {
                response($.map(data.filmes, function (value, key) {
                    return {
                        label: value,
                        value: value
                    };
                }));
            });
        },
        minLength: 3
    });
    
    $("#QuickSearchField").autocomplete({
        source: function (request, response) {
            $.getJSON("/Filmes/QuickSearch?query=" + request.term, function (data) {
                response($.map(data.filmes, function (item) {
                    return {
                        label: item.title,
                        value: item.id,
                        img: item.poster
                    };
                }));
            });
        },
        minLength: 3,
        select: function (event, ui) {
            var url = ui.item.value;
            if (url != '') {
                location.href = '/Filmes/Details/' + url;
            }
        },
        html: true,
        open: function (event, ui) {
            $(".ui-autocomplete").css("z-index", 10000);
        }
    }).autocomplete("instance")._renderItem = function (ul, item) {
        return $("<li><div><img src='" + item.img + "'><span>" + item.label + "</span></div></li>").appendTo(ul);
    };


    $("#Year").fastselect({ placeholder: 'Ano', searchPlaceholder: 'Buscar', noResultsText: 'Nenhum resultado' });
    $("#DirectorId").fastselect({ placeholder: 'Diretor', searchPlaceholder: 'Buscar', noResultsText: 'Nenhum resultado' });
    $("#CollectionId").fastselect({ placeholder: 'Coleção', searchPlaceholder: 'Buscar', noResultsText: 'Nenhum resultado' });
    $("#GenreIds").fastselect({ placeholder: 'Gênero', searchPlaceholder: 'Buscar', noResultsText: 'Nenhum resultado' });
    $("#ActorIds").fastselect({ placeholder: 'Atores', searchPlaceholder: 'Buscar', noResultsText: 'Nenhum resultado' });
    $('#SearchField').keyboard({
        usePreview: false, // disabled for contenteditable
        useCombos: false,
        autoAccept: true,
        openOn: null,
        stayOpen: true,
        layout: 'qwerty',
        position: {
            my: 'right bottom', at: 'right bottom', at2: 'right bottom', of: window, collision: 'flip flip'
        }
    }).autocomplete({
        source: function (request, response) {
            $.getJSON("/Filmes/GetFilmesJson?query=" + request.term, function (data) {
                response($.map(data.filmes, function (value, key) {
                    return {
                        label: value,
                        value: value
                    };
                }));
            });
        },
        minLength: 3
    }).addAutocomplete({
        // add autocomplete window positioning
        // options here (using position utility)
        position: {
            of: $('#SearchField'),
            my: 'center top',
            at: 'center bottom',
            collision: 'flipfit flipfit'
        }
    });

    $('#QuickSearchButton').click(function () {
        quickSearch = $("#QuickSearchField").val();
        if (quickSearch != '') {
            location.href = '/Filmes?SearchField=' + quickSearch;
        }
    });
});

function SortList(sortBy, sortOrder) {
    $('#parms_SortBy').val(sortBy);
    $('#parms_SortOrder').val(sortOrder);
    $('form').submit();
}

function OpenKeyboard() {
    $('#SearchField').getkeyboard().reveal();
}

function ExcluirFilme(id) {
    var confirm = window.confirm("Este filme será permanentemente excluído!\n\nTem certeza que deseja continuar?");
    if (confirm == true) {
        $.ajax({
            type: "POST",
            url: "/Filmes/ExcluirFilme",
            data: { id: id },
            success: function (data) {
                if (location.href.includes('/Details')) {
                    location.href = '/Filmes';
                }
                else {
                    location.reload();
                }                
            },
            error: function (error) {
                console.log(error);
                alert("Ocorreu um erro ao excluir o filme");
            }
        });
    }
}

function AddToWatchList(id) {
    $.ajax({
        type: "POST",
        url: "/Filmes/AddToWatchList",
        data: { id: id },
        success: function (data) {
            location.reload();
        },
        error: function (error) {
            console.log(error);
            alert("Ocorreu um erro ao adicionar o filme");
        }
    });
}

function RemoveFromWatchList(id) {
    $.ajax({
        type: "POST",
        url: "/Filmes/RemoveFromWatchList",
        data: { id: id },
        success: function (data) {
            location.reload();
        },
        error: function (error) {
            console.log(error);
            alert("Ocorreu um erro ao remover o filme");
        }
    });
}
