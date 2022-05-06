$(function () {

  /**
   * fetch source json
   */
  let host = location.host

  // Production home is https://malaybaku.github.io/VMagicMirror/
  if( host.includes( 'github.io' ) ){
    home = `https://${host}/VMagicMirror`;
  } else if( host.includes( 'localhost' ) ) {
    home = `http://${host}`
  }
  const lang = $('html').attr('lang');

  let source = [];
  $.get( `${home}/${lang}/api/search-source.json`, function (data) {
    source = data;
  });

  /**
   * Execute search.
   * Does not cancel Materialize js effect, using content editable div.
   */

  $('#search').on('keyup', function(){
    search( this.innerText, '#search-result');
    formCleaner( this.innerText, this.id );
  });

  $('#search-mobile').on('keyup', function(){
    search( this.innerText, '#search-result-mobile');
    formCleaner( this.innerText, this.id );
  });

  /**
   * editable content enable insert br tag but form is 1 line.
   */
  function formCleaner( keyword, id ){
    const target = '#' + id;
    $( target + ' br').remove();
    if ( keyword.length = 0 || keyword === '' ){
      $( target + ' *').remove();
    }
  }

  function search( searchWord, showResultTag ){
    const keyword = searchWord.toLowerCase().trim();

    if ( keyword.length > 0 && keyword !== '') {
      $(showResultTag).removeClass('hidden');
    } else {
      $(showResultTag + ' .result-item').remove();
      $(showResultTag).addClass('hidden');
      return;
    }

    $(showResultTag + ' .result-item').remove();

    const searchResult = source.reduce( (results, current) => {
        const content = current.content;

        if ( current.title.toLowerCase().indexOf(keyword) >= 0 ){
          current.content = current.content.substring( 0, 50 );
          results.push( current );
          return results;
        }

        const found = content.toLowerCase().indexOf(keyword);
        if ( found >= 0 ){
          current.content = current.content.substring( found - 10, found + 30 );
          results.push( current );
          return results;
        }

        return results;

    }, []);

    if (searchResult.length === 0) {
      $(showResultTag).append(
        '<div class="collection-header result-item">There is no search result.</div>'
      );
    } else {
      $(showResultTag).append(
        '<div class="collection-header result-item">Found pages</div>'
      );
      searchResult.forEach( item => {
        $(showResultTag).append(
          '<a class="collection-item result-item" href="' + item.url + '">' +
            item.title + '<br><span>' +
            item.content +
           '</span></a>'
        )
      });
    }
  }
});




