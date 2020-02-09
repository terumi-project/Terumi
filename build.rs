extern crate lalrpop;

fn main() {
    println!("build.rs running");
    lalrpop::process_root().unwrap();
}