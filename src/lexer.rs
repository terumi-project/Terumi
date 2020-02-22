use std::convert::TryFrom;

#[derive(Debug)]
pub enum Token<'a> {
  Identifier(Location<'a>),
  String(Location<'a>),
  Number(Location<'a>),
  Special(Location<'a>),
  Whitespace(Location<'a>),
}

// #[derive(Debug)]
pub struct Location<'a> {
  parent: &'a [u8],
  slice: &'a [u8],
  start: usize,
  end: usize
}

pub struct FileLocation {
  line: u32,
  column: u32,
}

impl<'a> Location<'a> {
  pub fn get_file_location(&self) -> FileLocation {
    // first, find and count up all the newlines up until the target location
    let mut last_newline_position = 0u32;
    let mut line_count = 1u32;

    for (i, element) in self.parent.iter().take(self.start).enumerate() {
      if *element == ('\n' as u8) {
        line_count += 1;
        last_newline_position = u32::try_from(i).unwrap();
      }
    };

    // subtraction yields how many column characters there are
    // adding one makes a nice number for displaying to the user
    let column_count: u32 = (u32::try_from(self.start).unwrap() - last_newline_position) + 1;

    FileLocation {
      line: line_count,
      column: column_count,
    }
  }
}

impl<'a> std::fmt::Debug for Location<'a> {
  fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
    write!(f, "Location {{ parent: {:p}, slice: {:?}, start: {}, end: {} }}", self.parent, String::from_utf8(Vec::from(self.slice)).unwrap(), self.start, self.end)
  }
}

enum NumberMatchResult {
  Number,
  Type(u8),
  Spacing,
  Decimal,
  None,
}

impl<'a> Location<'a> {
  pub fn new(parent: &'a [u8], range: std::ops::Range<usize>) -> Location<'a> {
    Location {
      start: range.start,
      end: range.end,
      parent: parent,
      slice: &parent[range],
    }
  }

  pub fn new_none(parent: &'a [u8], position: usize) -> Location<'a> {
    Location::new(parent, position..position)
  }
}

pub struct Lexer<'a> {
  data: &'a [u8],
  position: usize
}

impl<'a> Iterator for Lexer<'a> {
  type Item = Token<'a>;

  fn next(&mut self) -> Option<Token<'a>> {
    self.consume()
  }
}

impl<'a> Lexer<'a> {
  pub fn from_string(data: &'a str) -> Lexer<'a> {
    Lexer::from_bytes(data.as_bytes())
  }

  pub fn from_bytes(data: &'a [u8]) -> Lexer<'a> {
    Lexer {
      data: data,
      position: 0
    }
  }

  pub fn consume(&mut self) -> Option<Token<'a>> {
    let byte = self.poke_char()?;

    static N: u8 = '\n' as u8;
    match byte {
      byte if byte == N => Option::from(Token::Special(self.take_char())),
      byte if byte.is_ascii_whitespace() => Option::from(self.consume_whitespace()),
      byte if is_number(byte) => Option::from(self.consume_number()),
      byte if is_identifier(byte) => Option::from(self.consume_identifier()),
      _ => Option::from(Token::Special(self.take_char())),
    }
  }

  fn consume_whitespace(&mut self) -> Token<'a> {
    let mut end = self.position + 1;

    while self.poke_char_at(end).map_or(false, |v| v.is_ascii_whitespace()) {
      end += 1;
    }

    Token::Whitespace(self.generate_location(end))
  }

  fn consume_identifier(&mut self) -> Token<'a> {
    let mut end = self.position + 1;

    while self.poke_char_at(end).map_or(false, is_identifier) {
      end += 1;
    }
    
    Token::Identifier(self.generate_location(end))
  }

  fn consume_number(&mut self) -> Token<'a> {
    let mut end = self.position + 1;
    let mut has_deciaml = false;

    loop {
      let mut character: u8 = 0;
      let is_char = match self.poke_char_at(end) {
        Option::Some(c) => {
          character = c;
          true
        },
        _ => false,
      };

      if !is_char {
        break;
      }

      let char_match = match character as char {
        '0'..='9' => NumberMatchResult::Number,
        '.' => NumberMatchResult::Decimal,
        '_' => NumberMatchResult::Spacing,
        'x' | 'o' | 'b' => NumberMatchResult::Type(character),
        _ => NumberMatchResult::None,
      };

      /*
      let advance = match char_match {
        NumberMatchResult::Number | NumberMatchResult::Spacing => true,
        NumberMatchResult::Decimal => {
          if has_deciaml {
            // TODO: complain or something
          }

          has_deciaml = true;
          true
        }
      };*/

      end = end + 1;
    }

    Token::Number(self.generate_location(end))
  }

  fn take_char(&mut self) -> Location<'a> {
    self.generate_location(self.position + 1)
  }

  fn poke_char(&mut self) -> Option<u8> {
    char_at(self.data, self.position)
  }

  fn poke_char_at(&mut self, position: usize) -> Option<u8> {
    char_at(self.data, position)
  }

  fn generate_location(&mut self, took_until: usize) -> Location<'a> {
    let location = Location::new(self.data, self.position..took_until);

    self.position = took_until;

    location
  }
}

/// Valid characters for identifiers
fn is_identifier(data: u8) -> bool {
  match data as char {
    | 'A'..='Z'
    | 'a'..='z'
    | '_'
    // we can include numbers because we would detect numbers first
    | '0'..='9' => true,
    _ => false,
  }
}

fn is_number(data: u8) -> bool {
  match data as char {
    | '0'..='9' => true,
    _ => false,
  }
}

/// Gets a single character at a position in a string
fn char_at<'a>(data: &'a [u8], position: usize) -> Option<u8> {
  if data.len() <= position {
    None
  } else {
    Some(data[position])
  }
}